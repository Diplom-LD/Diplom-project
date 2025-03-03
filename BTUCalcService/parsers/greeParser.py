import asyncio
import aiohttp
from selectolax.parser import HTMLParser
from services.db import get_mongo_client
from services.mongodb_saver import MongoDBParserSaver


class GreeParser:
    base_url = "https://gree.com.md/ru/"

    async def fetch(self, session, url):
        """Запрашиваем страницу."""
        try:
            async with session.get(url) as response:
                if response.status != 200:
                    print(f"[ОШИБКА] Ошибка запроса {url}: {response.status}")
                    return None
                return await response.text()
        except Exception as e:
            print(f"[ОШИБКА] Ошибка запроса {url}: {e}")
            return None

    async def parse_products(self, html):
        """Парсим страницу и собираем товары."""
        tree = HTMLParser(html)
        products = []

        # Ищем все строки с товарами
        product_rows = tree.css("tr.line_prod.transition")

        for row in product_rows:
            try:
                # Название кондиционера и URL
                name_element = row.css_first("a.line_prod_title")
                name = name_element.text(strip=True) if name_element else None
                url = "https://gree.com.md" + name_element.attributes.get("href", "") if name_element else None

                # Получаем все <td> внутри строки товара
                columns = row.css("td")

                # Площадь обслуживания (2-й <td>)
                service_area = None
                if len(columns) > 1:
                    service_area_text = columns[1].text(strip=True)
                    try:
                        service_area = float(service_area_text.replace("м²", "").strip()) if service_area_text.replace(".", "", 1).isdigit() else None
                    except ValueError:
                        service_area = None

                # BTU (3-й <td>)
                btu = None
                if len(columns) > 2:
                    btu_text = columns[2].text(strip=True).replace(" ", "")
                    try:
                        btu = int(btu_text) if btu_text.isdigit() else None
                    except ValueError:
                        btu = None

                # Цена (4-й <td>)
                price = None
                currency = "MDL"
                if len(columns) > 3:
                    price_element = columns[3].css_first("a")
                    if price_element:
                        price_text = price_element.text(strip=True).replace("лей", "").replace(" ", "")
                        try:
                            price = int(price_text) if price_text.isdigit() else None
                        except ValueError:
                            price = None

                if name and url:
                    products.append({
                        "name": name,
                        "url": url,
                        "price": price,
                        "currency": currency,
                        "btu": btu,
                        "service_area": service_area,
                        "store": "gree"
                    })

            except Exception as e:
                print(f"[ОШИБКА] Ошибка при парсинге товара: {e}")

        if not products:
            print(f"[ПРЕДУПРЕЖДЕНИЕ] На {self.base_url} не найдено товаров.")

        return products

    async def run(self):
        """Главная функция парсинга."""
        async with aiohttp.ClientSession() as session:
            print(f"Парсим страницу: {self.base_url}")
            html = await self.fetch(session, self.base_url)

            if html is None:
                print(f"[ОШИБКА] Ошибка загрузки {self.base_url}")
                return []

            products = await self.parse_products(html)

            print(f"Собрано {len(products)} товаров")
            return products


async def main():
    parser = GreeParser()
    results = await parser.run()

    db = get_mongo_client()
    saver = MongoDBParserSaver(db)

    # Сохраняем в коллекцию конкретного парсера
    saver.save_products("gree", results)

    # Сохраняем в общую коллекцию all_products
    saver.save_products("all", results)

    print("\nИТОГОВЫЙ СПИСОК ТОВАРОВ:")
    for item in results:
        print(item)


if __name__ == "__main__":
    asyncio.run(main())
