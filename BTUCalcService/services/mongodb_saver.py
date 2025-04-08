import hashlib
import json
from datetime import datetime
from pymongo.collection import Collection
from pymongo.database import Database
from pymongo.errors import BulkWriteError
from pymongo import ReplaceOne


def calculate_overall_hash(products: list[dict]) -> str:
    """Вычисляет хэш всех товаров."""
    data_to_hash = []
    for product in products:
        clean_product = {k: v for k, v in product.items() if k not in {"_id", "hash", "updated_at"}}
        data_to_hash.append(clean_product)

    data_string = json.dumps(data_to_hash, sort_keys=True, ensure_ascii=False)
    return hashlib.sha256(data_string.encode()).hexdigest()


class MongoDBParserSaver:
    def __init__(self, db: Database):
        self.db = db

    def save_products(self, parser_name: str, products: list[dict]) -> bool:
        """Сохраняет продукты в базу данных."""
        collection: Collection = self.db[f"{parser_name}_products"]
        all_products_collection: Collection = self.db["all_products"]

        if not products:
            print(f"[{parser_name}] ❌ Пустой список товаров. Пропуск сохранения.")
            return False

        overall_hash = calculate_overall_hash(products)
        print(f"[{parser_name}] 🔐 Новый хэш: {overall_hash}")

        metadata = collection.find_one({"_id": "metadata"})
        current_db_hash = metadata["hash"] if metadata else None
        print(f"[{parser_name}] 📦 Текущий хэш в БД: {current_db_hash}")

        if current_db_hash == overall_hash:
            print(f"[{parser_name}] ℹ️ Хэш не изменился. Обновление не требуется.")
            return False

        print(f"[{parser_name}] 🔁 Хэш изменился. Начинаем обновление...")

        collection.delete_many({"_id": {"$ne": "metadata"}})

        collection.update_one(
            {"_id": "metadata"},
            {"$set": {"hash": overall_hash, "updated_at": datetime.utcnow()}},
            upsert=True
        )

        bulk_operations = []
        bulk_operations_all = []

        updated_count = 0
        inserted_count = 0

        for product in products:
            if "url" not in product or not product["url"]:
                print(f"[{parser_name}] ⚠️ Пропущен товар без 'url': {product}")
                continue

            product["_id"] = product["url"]
            product["updated_at"] = datetime.utcnow()

            if collection.find_one({"_id": product["_id"]}):
                updated_count += 1
            else:
                inserted_count += 1

            bulk_operations.append(
                ReplaceOne({"_id": product["_id"]}, product, upsert=True)
            )

            product_copy = product.copy()
            product_copy["_id"] = f"{parser_name}_{product['url']}"
            product_copy["source"] = parser_name
            bulk_operations_all.append(
                ReplaceOne({"_id": product_copy["_id"]}, product_copy, upsert=True)
            )

        try:
            if bulk_operations:
                result = collection.bulk_write(bulk_operations)
                print(f"[{parser_name}] ✅ Обновлено {updated_count}, добавлено {inserted_count} товаров.")

            if bulk_operations_all:
                # Удаляем старые товары этого парсера в общей коллекции
                all_products_collection.delete_many({"source": parser_name})
                result_all = all_products_collection.bulk_write(bulk_operations_all)
                print(f"[all_products] ✅ Записано {len(bulk_operations_all)} товаров из {parser_name}.")
                print(f"[all_products] MongoDB результат: {result_all.bulk_api_result}")

        except BulkWriteError as e:
            print(f"[{parser_name}] ❌ Ошибка массовой записи: {e.details}")
            return False

        return True
