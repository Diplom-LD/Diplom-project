import asyncio
import aiohttp
from selectolax.parser import HTMLParser
from services.db import get_mongo_client
from services.mongodb_saver import MongoDBParserSaver


class JaraParser:
    base_url = "https://jara.md/ru/kondicionery-i-klimaticheskie-ustanovki/bytovye-kondicionery/p"

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

        product_links = tree.css("div.list-prod-bloc-titlu a")
        for link in product_links:
            name = link.attributes.get("title")
            url = link.attributes.get("href")
            if name and url:
                products.append({"name": name, "url": url})

        # Определяем последнюю страницу
        last_page_number = 1
        last_page_element = tree.css("a.page_link")
        for page_link in last_page_element:
            try:
                page_text = page_link.text(strip=True)
                if page_text.isdigit():
                    last_page_number = max(last_page_number, int(page_text))
            except ValueError:
                pass

        return products, last_page_number

    async def parse_product_page(self, session, product):
        html = await self.fetch(session, product["url"])
        if not html:
            print(f"[ОШИБКА] Не удалось загрузить страницу товара {product['url']}")
            return None

        tree = HTMLParser(html)

        # Перепроверка имени
        name_element = tree.css_first("h1.text-xl")
        if name_element:
            product["name"] = name_element.text(strip=True)

        # Цена и валюта
        price_element = tree.css_first("div.pret_final_prod_cur")
        if price_element:
            price_text = price_element.text(strip=True).replace("лей", "").replace(" ", "").strip()
            try:
                product["price"] = int(price_text) if price_text.isdigit() else None
            except ValueError:
                product["price"] = None
        else:
            product["price"] = None

        product["currency"] = "MDL"

        # BTU
        product["btu"] = None
        for p in tree.css("p"):
            if "Мощность, BTU" in p.text():
                strong_element = p.next
                while strong_element and strong_element.tag != "strong":
                    strong_element = strong_element.next
                if strong_element and strong_element.tag == "strong":
                    btu_text = strong_element.text(strip=True)
                    if btu_text.isdigit():
                        product["btu"] = int(btu_text)

        # Площадь
        product["service_area"] = None
        for p in tree.css("p"):
            if "Площадь помещения, м²" in p.text():
                strong_element = p.next
                while strong_element and strong_element.tag != "strong":
                    strong_element = strong_element.next
                if strong_element and strong_element.tag == "strong":
                    area_text = strong_element.text(strip=True)
                    try:
                        product["service_area"] = float(area_text.replace("м²", "").strip()) if area_text.replace(".", "", 1).isdigit() else None
                    except ValueError:
                        product["service_area"] = None

        # Магазин
        product["store"] = "jara"

        return product

    async def run(self):
        """Главная функция парсинга."""
        products = []

        async with aiohttp.ClientSession() as session:
            first_page_url = f"{self.base_url}/1"
            print(f"Парсим страницу: {first_page_url}")
            first_page_html = await self.fetch(session, first_page_url)
            if not first_page_html:
                print("[ОШИБКА] Ошибка загрузки первой страницы")
                return []

            page_products, last_page_number = await self.parse_list_page(first_page_html)
            products.extend(page_products)

            print(f"Определено страниц: {last_page_number}")

            for page in range(2, last_page_number + 1):
                url = f"{self.base_url}/{page}"
                print(f"Парсим страницу: {url}")
                html = await self.fetch(session, url)
                if not html:
                    continue
                page_products, _ = await self.parse_list_page(html)
                products.extend(page_products)

            print(f"Собрано {len(products)} товаров для детального парсинга.")

            detailed_products = []
            for i, product in enumerate(products, start=1):
                detailed_product = await self.parse_product_page(session, product)
                if detailed_product:
                    detailed_products.append(detailed_product)
                    print(
                        f"[{i}/{len(products)}] {detailed_product['name']} – {detailed_product['price']} {detailed_product['currency']} | BTU: {detailed_product['btu']} | Площадь: {detailed_product['service_area']} | Магазин: {detailed_product['store']} | URL: {detailed_product['url']}"
                    )

            return detailed_products


async def main():
    parser = JaraParser()
    results = await parser.run()

    db = get_mongo_client()
    saver = MongoDBParserSaver(db)

    # Сохранение в коллекцию конкретного парсера
    saver.save_products("jara", results)

    # Сохранение в общую коллекцию all_products
    saver.save_products("all", results)

    print("\nИТОГОВЫЙ СПИСОК ТОВАРОВ:")
    for item in results:
        print(item)


if __name__ == "__main__":
    asyncio.run(main())
