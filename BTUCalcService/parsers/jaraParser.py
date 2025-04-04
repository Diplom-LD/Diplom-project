import asyncio
import aiohttp
from selectolax.parser import HTMLParser
from services.db import get_mongo_client
from services.mongodb_saver import MongoDBParserSaver


class JaraParser:
    base_url = "https://jara.md/ru/bytovye-kondicionery/?page="

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

    async def get_last_page_number(self, html):
        tree = HTMLParser(html)
        pagination = tree.css_first("ul.pagination.df.ac")
        if not pagination:
            print("[ОШИБКА] Блок пагинации не найден")
            return 1

        page_links = pagination.css("li")
        if len(page_links) < 2:
            print("[ОШИБКА] Недостаточно ссылок в пагинации")
            return 1

        # Предпоследний <li> содержит номер последней страницы
        last_page_li = page_links[-2]
        link = last_page_li.css_first("a.pagelink")
        if not link:
            print("[ОШИБКА] Не удалось найти ссылку в предпоследнем элементе пагинации")
            return 1

        try:
            last_page_number = int(link.text(strip=True))
            return last_page_number
        except ValueError:
            print("[ОШИБКА] Не удалось распарсить номер последней страницы")
            return 1

    async def parse_list_page(self, html):
        tree = HTMLParser(html)
        products = []

        product_cards = tree.css("div.prod_card")
        for card in product_cards:
            link_element = card.css_first("a.pcard_top")
            title_element = card.css_first("span.pcard_title")
            price_element = card.css_first("div.pcard_price")

            url = link_element.attributes.get("href") if link_element else None
            name = title_element.text(strip=True) if title_element else None
            price_text = price_element.text(strip=True).replace("лей", "").replace(" ", "") if price_element else None

            try:
                price = int(price_text) if price_text and price_text.isdigit() else None
            except ValueError:
                price = None

            if name and url:
                products.append({
                    "name": name,
                    "url": url if url.startswith("http") else f"https://jara.md{url}",
                    "price": price,
                    "currency": "MDL",
                    "store": "jara"
                })

        return products

    async def parse_product_page(self, session, product):
        html = await self.fetch(session, product["url"])
        if not html:
            print(f"[ОШИБКА] Не удалось загрузить страницу товара {product['url']}")
            return None

        tree = HTMLParser(html)

        # Перепроверка имени
        name_element = tree.css_first("h1.prod_title")
        if name_element:
            product["name"] = name_element.text(strip=True)

        # Перепроверка цены
        price_element = tree.css_first("div.pd_price")
        if price_element:
            price_text = price_element.text(strip=True).replace("лей", "").replace(" ", "").strip()
            try:
                product["price"] = int(price_text) if price_text.isdigit() else None
            except ValueError:
                product["price"] = None

        # BTU и площадь
        param_blocks = tree.css("div.pd_params_row")
        product["btu"] = None
        product["service_area"] = None

        for block in param_blocks:
            title_el = block.css_first("div.pd_param_title")
            value_el = block.css_first("div.pd_param_value")
            if not title_el or not value_el:
                continue

            title = title_el.text(strip=True).lower().replace(" ", "").replace(" ", "")
            value = value_el.text(strip=True).replace(" ", "").replace(" ", "")

            if "мощность,btu" in title:
                try:
                    product["btu"] = int(value)
                except ValueError:
                    product["btu"] = None

            if "площадьпомещения" in title:
                try:
                    product["service_area"] = float(value.replace(",", "."))
                except ValueError:
                    product["service_area"] = None

        return product

    async def run(self):
        """Главная функция парсинга."""
        products = []

        async with aiohttp.ClientSession() as session:
            first_page_url = f"{self.base_url}1"
            print(f"Парсим первую страницу: {first_page_url}")
            first_page_html = await self.fetch(session, first_page_url)
            if not first_page_html:
                print("[ОШИБКА] Ошибка загрузки первой страницы")
                return []

            last_page_number = await self.get_last_page_number(first_page_html)
            print(f"Найдено страниц: {last_page_number}")

            # Парсим первую страницу
            first_page_products = await self.parse_list_page(first_page_html)
            products.extend(first_page_products)

            # Остальные страницы
            for page in range(2, last_page_number + 1):
                url = f"{self.base_url}{page}"
                print(f"Парсим страницу: {url}")
                html = await self.fetch(session, url)
                if not html:
                    continue
                page_products = await self.parse_list_page(html)
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
