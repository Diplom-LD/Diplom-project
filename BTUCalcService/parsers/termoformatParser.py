import asyncio
import aiohttp
import re
from selectolax.parser import HTMLParser
from services.db import get_mongo_client
from services.mongodb_saver import MongoDBParserSaver


class TermoformatParser:
    base_url = "https://termoformat.md"

    async def fetch(self, session, url):
        """Функция запроса страницы."""
        try:
            async with session.get(url, timeout=10) as response:
                if response.status != 200:
                    print(f"[ОШИБКА] Ошибка запроса {url}: {response.status}")
                    return None
                return await response.text()
        except asyncio.TimeoutError:
            print(f"[ОШИБКА] Тайм-аут при запросе {url}")
            return None
        except aiohttp.ClientError as e:
            print(f"[ОШИБКА] Сетевая ошибка при запросе {url}: {e}")
            return None
        except Exception as e:
            print(f"[ОШИБКА] Неизвестная ошибка при запросе {url}: {e}")
            return None

    async def parse_list_page(self, html):
        """Парсим страницу списка товаров, собираем ссылки на товары и проверяем наличие следующей страницы."""
        tree = HTMLParser(html)
        products = []

        product_elements = tree.css("div.product-info a.product-name.nolink")
        for product in product_elements:
            url = product.attributes.get("href")
            name_element = product.css_first("span[itemprop='name']")
            name = name_element.text(strip=True) if name_element else None

            if url and name:
                if not url.startswith("http"):
                    url = self.base_url + url
                products.append({
                    "url": url,
                    "name": name
                })

        next_page_element = tree.css_first("div.pagination a.arrow.right")
        next_page_url = next_page_element.attributes.get("href") if next_page_element else None
        if next_page_url and not next_page_url.startswith("http"):
            next_page_url = self.base_url + next_page_url

        return products, next_page_url

    async def parse_product_page(self, session, product):
        """Парсим детальную информацию о товаре."""
        html = await self.fetch(session, product["url"])
        if not html:
            print(f"[ОШИБКА] Не удалось загрузить страницу товара {product['url']}")
            return None

        try:
            tree = HTMLParser(html)

            # Перепроверка имени товара
            name_element = tree.css_first("span[itemprop='name']")
            if name_element:
                product["name"] = name_element.text(strip=True)

            # Цена и валюта
            price_element = tree.css_first("div.main-price span[itemprop='price']")
            currency_element = tree.css_first("div.main-price small[itemprop='priceCurrency']")

            try:
                product["price"] = int(price_element.text(strip=True).replace(" ", "").strip()) if price_element else None
            except ValueError:
                product["price"] = None

            product["currency"] = currency_element.text(strip=True).strip() if currency_element else "MDL"

            # Инициализация BTU и Площади
            product["btu"] = None
            product["service_area"] = None

            # Проходим по всем строкам таблицы с характеристиками
            for row in tree.css("tr"):
                param_name = row.css_first("td.param-name")
                param_value = row.css_first("td.param-value")

                if not param_name or not param_value:
                    continue

                param_name_text = param_name.text(strip=True)
                param_value_text = param_value.text(strip=True)

                # Извлечение значения BTU
                if "Производительность" in param_name_text and "BTU" in param_value_text:
                    btu_value = param_value_text.split("BTU")[0].strip()
                    btu_digits = "".join(filter(str.isdigit, btu_value))
                    try:
                        product["btu"] = int(btu_digits)
                    except ValueError:
                        product["btu"] = None

                # Извлечение рекомендуемой площади
                if "Рекомендуемая площадь" in param_name_text:
                    # Убираем знак "²" и оставляем только цифры
                    area_digits = "".join(filter(str.isdigit, param_value_text.replace("²", "")))
                    if area_digits.isdigit():
                        product["service_area"] = f"{area_digits} м²"

            product["store"] = "termoformat"

            # Преобразуем все числовые значения
            if product["price"]:
                product["price"] = int(product["price"])  # Преобразуем цену в int
            if product["btu"]:
                product["btu"] = int(product["btu"])  # Преобразуем BTU в int
            if product["service_area"]:
                try:
                    product["service_area"] = float(product["service_area"].replace("м²", "").strip())  # Преобразуем площадь в float
                except ValueError:
                    product["service_area"] = None

            return product

        except Exception as e:
            print(f"[ОШИБКА] Ошибка при парсинге товара {product['url']}: {e}")
            return None

    async def run(self):
        products = []

        async with aiohttp.ClientSession() as session:
            next_page_url = f"{self.base_url}/ru/kondicioneri/split_sistemi/1"

            # Сбор всех товаров со всех страниц
            while next_page_url:
                print(f"Парсим страницу: {next_page_url}")
                html = await self.fetch(session, next_page_url)
                if not html:
                    print(f"[ОШИБКА] Не удалось загрузить страницу {next_page_url}")
                    break

                page_products, next_page_url = await self.parse_list_page(html)
                products.extend(page_products)

            print(f"Собрано {len(products)} товаров для детального парсинга.")

            # Подробный парсинг карточек товаров
            detailed_products = []
            for i, product in enumerate(products, start=1):
                detailed_product = await self.parse_product_page(session, product)
                if detailed_product:
                    detailed_products.append(detailed_product)
                    print(
                        f"[{i}/{len(products)}] {detailed_product['name']} – {detailed_product['price']} {detailed_product['currency']} | "
                        f"BTU: {detailed_product['btu']} | Площадь: {detailed_product['service_area']} | Магазин: {detailed_product['store']} | URL: {detailed_product['url']}"
                    )

            print(f"\nСобрано детально {len(detailed_products)} товаров")
            return detailed_products


async def main():
    parser = TermoformatParser()
    results = await parser.run()

    db = get_mongo_client()
    saver = MongoDBParserSaver(db)

    # Сохранение в коллекцию конкретного парсера
    saver.save_products("termoformat", results)

    # Сохранение в общую коллекцию all_products
    saver.save_products("all", results)

    print("\nИТОГОВЫЙ СПИСОК ТОВАРОВ:")
    for item in results:
        print(item)


if __name__ == "__main__":
    asyncio.run(main())
