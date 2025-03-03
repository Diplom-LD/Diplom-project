import asyncio
import aiohttp
from selectolax.parser import HTMLParser
from services.db import get_mongo_client
from services.mongodb_saver import MongoDBParserSaver


class EurosantehParser:
    base_url = "https://eurosanteh.md"
    start_url = "https://eurosanteh.md/ru/nastennye-kondicionery-split-sistemy/?page=1"

    async def fetch(self, session, url):
        try:
            async with session.get(url) as response:
                if response.status != 200:
                    print(f"[ОШИБКА] Ошибка запроса {url}: {response.status}")
                    return None
                return await response.text()
        except Exception as e:
            print(f"[ОШИБКА] Ошибка запроса {url}: {e}")
            return None

    async def parse_list_page(self, html):
        tree = HTMLParser(html)
        products = []

        product_cards = tree.css("div.prod_card")
        for card in product_cards:
            try:
                # Имя и URL
                name_element = card.css_first("a.prod_title")
                if not name_element:
                    continue

                url = self.base_url + name_element.attributes.get("href")
                name = name_element.text(strip=True)

                # BTU и Площадь
                btu = None
                service_area = None
                param_rows = card.css("div.prod_param_row")
                for row in param_rows:
                    title_element = row.css_first("div.prod_param_title")
                    value_element = row.css_first("div.prod_param_value")

                    if not title_element or not value_element:
                        continue

                    title_text = title_element.text(strip=True)
                    value_text = value_element.text(strip=True)

                    if "Мощность" in title_text and "BTU" in title_text:
                        try:
                            btu = int(value_text.replace(' ', '').strip())  # Преобразуем в целое число
                        except ValueError:
                            btu = None

                    if "Площадь помещения" in title_text:
                        service_area_text = value_text.strip().replace("м²", "").strip()
                        try:
                            service_area = float(service_area_text) if service_area_text.replace('.', '', 1).isdigit() else None  # Преобразуем в число
                        except ValueError:
                            service_area = None

                # Цена и валюта
                price_element = card.css_first("div.prod_price")
                price = None
                currency = "MDL"

                if price_element:
                    price_text = price_element.text(strip=True)
                    price_digits = "".join(filter(str.isdigit, price_text))
                    if price_digits.isdigit():
                        price = int(price_digits)  # Преобразуем цену в целое число

                products.append({
                    "name": name,
                    "url": url,
                    "price": price,
                    "currency": currency,
                    "btu": btu,
                    "service_area": service_area,
                    "store": "eurosanteh"
                })

            except Exception as e:
                print(f"[ОШИБКА] Ошибка парсинга товара: {e}")

        # Определяем последнюю страницу
        last_page_number = 1
        pagination_links = tree.css("ul.pagination a.pagelink")
        for link in pagination_links:
            try:
                page_number = int(link.text(strip=True))
                last_page_number = max(last_page_number, page_number)
            except ValueError:
                pass

        return products, last_page_number

    async def run(self):
        products = []

        async with aiohttp.ClientSession() as session:
            print(f"Парсим страницу: {self.start_url}")
            first_page_html = await self.fetch(session, self.start_url)

            if not first_page_html:
                print("[ОШИБКА] Не удалось загрузить первую страницу")
                return []

            first_page_products, last_page_number = await self.parse_list_page(first_page_html)
            products.extend(first_page_products)

            print(f"Определено страниц: {last_page_number}")

            # Парсим оставшиеся страницы
            for page in range(2, last_page_number + 1):
                page_url = f"https://eurosanteh.md/ru/nastennye-kondicionery-split-sistemy/?page={page}"
                print(f"Парсим страницу: {page_url}")

                html = await self.fetch(session, page_url)
                if not html:
                    print(f"[ОШИБКА] Не удалось загрузить страницу {page_url}")
                    continue

                try:
                    page_products, _ = await self.parse_list_page(html)
                    products.extend(page_products)
                except Exception as e:
                    print(f"[ОШИБКА] Ошибка обработки страницы {page_url}: {e}")

            print(f"Собрано {len(products)} товаров")

            return products


async def main():
    parser = EurosantehParser()
    results = await parser.run()

    db = get_mongo_client()
    saver = MongoDBParserSaver(db)

    # Сохраняем в коллекцию конкретного парсера
    saver.save_products("eurosanteh", results)

    # Сохраняем в общую коллекцию all_products
    saver.save_products("all", results)

    print("\nИТОГОВЫЙ СПИСОК ТОВАРОВ:")
    for item in results:
        print(item)


if __name__ == "__main__":
    asyncio.run(main())
