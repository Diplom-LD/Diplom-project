import asyncio
import aiohttp
from selectolax.parser import HTMLParser
from services.db import get_mongo_client
from services.mongodb_saver import MongoDBParserSaver
from datetime import datetime


class ConditionereParser:
    base_url = "https://conditionere.md"
    start_url = "https://conditionere.md/ru/nastennye-kondicionery/"

    async def fetch(self, session, url):
        try:
            async with session.get(url, timeout=10) as response:
                if response.status != 200:
                    print(f"[ОШИБКА] Ошибка запроса {url}: {response.status}")
                    return None
                return await response.text()
        except asyncio.TimeoutError:
            print(f"[ОШИБКА] Таймаут запроса {url}")
        except aiohttp.ClientError as e:
            print(f"[ОШИБКА] Ошибка сети {url}: {e}")
        except Exception as e:
            print(f"[ОШИБКА] Неизвестная ошибка при запросе {url}: {e}")
        return None

    async def parse_list_page(self, html):
        products = []
        last_page_number = 1

        try:
            tree = HTMLParser(html)
            product_cards = tree.css("div.prod_card.transition")

            for card in product_cards:
                try:
                    name_element = card.css_first("a.prod_card_title")
                    url = self.base_url + name_element.attributes.get("href")
                    name = name_element.text(strip=True)

                    # Цена
                    price_element = card.css_first("div.prod_card_price")
                    price_text = price_element.text(strip=True).replace("лей", "").replace(" ", "").strip() if price_element else None
                    price = float(price_text) if price_text and price_text.replace('.', '', 1).isdigit() else None  # Преобразуем в число

                    # BTU и Площадь
                    btu = None
                    service_area = None

                    spec_rows = card.css("div.pcp_row")
                    for row in spec_rows:
                        title_element = row.css_first("div.pcp_title")
                        value_element = row.css_first("div.pcp_value")
                        if not title_element or not value_element:
                            continue

                        title_text = title_element.text(strip=True)
                        value_text = value_element.text(strip=True)

                        if "Мощность" in title_text and "BTU" in title_text:
                            try:
                                btu = int(value_text.replace(' ', '').strip())  # Преобразуем в целое число
                            except ValueError:
                                btu = None

                        elif "Площадь помещения" in title_text:
                            service_area_text = value_text.strip().replace("м²", "").strip()
                            try:
                                service_area = float(service_area_text) if service_area_text.replace('.', '', 1).isdigit() else None  # Преобразуем в число
                            except ValueError:
                                service_area = None

                    products.append({
                        "name": name,
                        "url": url,
                        "price": price,
                        "currency": "MDL",
                        "btu": btu,
                        "service_area": service_area,
                        "store": "conditionere",
                        "updated_at": datetime.utcnow(),
                    })
                except Exception as e:
                    print(f"[ОШИБКА] Ошибка парсинга товара: {e}")

            # Определяем последнюю страницу
            pagination_links = tree.css("ul.pagination a.pagelink")
            for link in pagination_links:
                try:
                    page_number = int(link.text(strip=True))
                    last_page_number = max(last_page_number, page_number)
                except ValueError:
                    continue

        except Exception as e:
            print(f"[ОШИБКА] Ошибка парсинга страницы со списком товаров: {e}")

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
                page_url = f"{self.start_url}?page={page}"
                print(f"Парсим страницу: {page_url}")

                html = await self.fetch(session, page_url)
                if not html:
                    print(f"[ОШИБКА] Не удалось загрузить страницу {page_url}")
                    continue

                page_products, _ = await self.parse_list_page(html)
                products.extend(page_products)

            print(f"Собрано {len(products)} товаров")

            return products


async def main():
    parser = ConditionereParser()
    results = await parser.run()

    if not results:
        print("[ОШИБКА] Нет данных для сохранения.")
        return

    db = get_mongo_client()
    saver = MongoDBParserSaver(db)

    # 1. Сохраняем в коллекцию конкретного парсера
    saver.save_products("conditionere", results)

    # 2. Сохраняем/обновляем в общую коллекцию all_products
    saver.save_products("all", results)

    print("\nИТОГОВЫЙ СПИСОК ТОВАРОВ:")
    for item in results:
        print(item)


if __name__ == "__main__":
    asyncio.run(main())
