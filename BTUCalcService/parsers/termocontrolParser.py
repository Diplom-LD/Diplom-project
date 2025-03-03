import asyncio
import aiohttp
import re
from selectolax.parser import HTMLParser
from services.db import get_mongo_client
from services.mongodb_saver import MongoDBParserSaver

class TermoControlParser:
    base_url = "https://termocontrol.md/ru/catalog/split"

    async def fetch(self, session, url):
        """Функция запроса страницы с таймаутом и обработкой ошибок."""
        try:
            async with session.get(url, timeout=10) as response:
                if response.status == 404:
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
        """Собираем name и url с одной страницы каталога."""
        try:
            tree = HTMLParser(html)
            products = []

            product_links = tree.css("a.product_preview__name_link")
            for link in product_links:
                try:
                    name = link.text(strip=True)
                    url = "https://termocontrol.md" + link.attributes.get("href")
                    products.append({"name": name, "url": url})
                except Exception as e:
                    print(f"[ОШИБКА] Ошибка парсинга товара: {e}")

            return products
        except Exception as e:
            print(f"[ОШИБКА] Ошибка парсинга страницы списка: {e}")
            return []

    async def parse_product_page(self, session, product):
        """Переход по ссылке товара и сбор данных."""
        try:
            html = await self.fetch(session, product["url"])
            if html is None:
                print(f"[ОШИБКА] Не удалось загрузить страницу товара {product['url']}")
                return None

            tree = HTMLParser(html)

            # Перепроверка имени
            name_element = tree.css_first("h1.block__heading span[itemprop='name']")
            if name_element:
                product["name"] = name_element.text(strip=True)

            # Цена и валюта
            price_element = tree.css_first("span.fn_price[itemprop='price']")
            currency_element = tree.css_first("span.currency[itemprop='priceCurrency']")

            try:
                product["price"] = int(price_element.attributes.get("content", "0")) if price_element else None
            except ValueError:
                product["price"] = None

            product["currency"] = currency_element.text(strip=True) if currency_element else "MDL"

            # Инициализируем BTU и площадь
            product["btu"] = None
            product["service_area"] = None

            # Ищем BTU
            for name_div, value_div in zip(tree.css("div.features__name"), tree.css("div.features__value")):
                name_text = name_div.text(strip=True).lower()
                value_text = value_div.text(strip=True)

                if "btu" in name_text or "произв" in name_text:
                    product["btu"] = self.extract_max_number(value_text, "BTU")

                if "площадь" in name_text or "suprafața" in name_text:
                    product["service_area"] = f"{self.extract_max_number(value_text)} м²"

            product["store"] = "termocontrol"

            # Преобразуем все числовые значения
            if product["price"]:
                product["price"] = int(product["price"]) 
            if product["btu"]:
                product["btu"] = int(product["btu"])  
            if product["service_area"]:
                try:
                    product["service_area"] = float(product["service_area"].replace("м²", "").strip())  # Преобразуем площадь в float
                except ValueError:
                    product["service_area"] = None

            return product

        except Exception as e:
            print(f"[ОШИБКА] Ошибка парсинга товара {product['url']}: {e}")
            return None

    async def run(self):
        products = []
        async with aiohttp.ClientSession() as session:
            page = 1

            # 1. Сбор списка всех товаров
            while True:
                if page == 1:
                    url = self.base_url
                else:
                    url = f"{self.base_url}/page-{page}"

                print(f"Парсим страницу: {url}")
                html = await self.fetch(session, url)

                if html is None:
                    print(f"Страница {page} не найдена (404). Заканчиваем парсинг.")
                    break

                page_products = await self.parse_list_page(html)
                if not page_products:
                    print(f"Пустая страница {page}. Возможно, товары закончились.")
                    break

                products.extend(page_products)
                page += 1

            print(f"Собрано {len(products)} товаров для детального парсинга.")

            # 2. Парсим детали товаров
            detailed_products = []
            for i, product in enumerate(products, start=1):
                detailed_product = await self.parse_product_page(session, product)
                await asyncio.sleep(0.2)
                if detailed_product:
                    detailed_products.append(detailed_product)
                    print(
                        f"[{i}/{len(products)}] {detailed_product['name']} – {detailed_product['price']} {detailed_product['currency']} | "
                        f"BTU: {detailed_product['btu']} | Площадь: {detailed_product['service_area']} | Магазин: {detailed_product['store']} | URL: {detailed_product['url']}"
                    )

            print(f"\nСобрано детально {len(detailed_products)} товаров")
            return detailed_products

    def extract_max_number(self, text, unit=""):
        """Извлекает наибольшее число из строки, игнорируя все символы, кроме чисел."""
        # Ищем все числа в строке
        numbers = re.findall(r'\d+', text)

        if numbers:
            # Преобразуем все найденные числа в целые и возвращаем максимальное
            return max(map(int, numbers))  # Возвращаем максимальное число
        return None



async def main():
    parser = TermoControlParser()
    results = await parser.run()

    db = get_mongo_client()
    saver = MongoDBParserSaver(db)

    saver.save_products("termocontrol", results)
    saver.save_products("all", results)

    print("\nИТОГОВЫЙ СПИСОК ТОВАРОВ:")
    for item in results:
        print(item)


if __name__ == "__main__":
    asyncio.run(main())
