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
        # Исключаем поля, которые не влияют на хэш
        clean_product = {k: v for k, v in product.items() if k not in {"_id", "hash", "updated_at"}}
        data_to_hash.append(clean_product)

    # Преобразуем в строку и вычисляем хэш
    data_string = json.dumps(data_to_hash, sort_keys=True, ensure_ascii=False)
    return hashlib.sha256(data_string.encode()).hexdigest()


class MongoDBParserSaver:
    def __init__(self, db: Database):
        self.db = db

    def save_products(self, parser_name: str, products: list[dict]) -> bool:
        """Сохраняет продукты в базе данных."""
        collection: Collection = self.db[f"{parser_name}_products"]
        all_products_collection = self.db["all_products"]

        if not products:
            print(f"[{parser_name}] Пустой список товаров. Пропуск сохранения.")
            return False

        # Вычисляем новый хэш для данных товаров
        overall_hash = calculate_overall_hash(products)
        print(f"[{parser_name}] Новый хэш: {overall_hash}")

        # Получаем текущий хэш из метаданных
        metadata = collection.find_one({"_id": "metadata"})
        current_db_hash = metadata["hash"] if metadata else None
        print(f"[{parser_name}] Текущий хэш в базе данных: {current_db_hash}")

        # Если хэш не изменился, пропускаем обновление
        if current_db_hash == overall_hash:
            print(f"[{parser_name}] Хэш не изменился. Пропускаем обновление.")
            return False

        print(f"[{parser_name}] Хэш изменился. Начинаем перезапись данных...")

        # Удаляем все товары, кроме метаданных
        collection.delete_many({"_id": {"$ne": "metadata"}})

        # Обновляем метаданные с новым хэшем
        collection.update_one(
            {"_id": "metadata"},
            {"$set": {"hash": overall_hash, "updated_at": datetime.utcnow()}},
            upsert=True
        )

        bulk_operations = []
        bulk_operations_all = []

        updated_count = 0  # Считаем обновленные товары
        inserted_count = 0  # Считаем добавленные товары

        # Подготовка операций для массовой записи
        for product in products:
            product["_id"] = product["url"]
            product["updated_at"] = datetime.utcnow()

            existing_product = collection.find_one({"_id": product["_id"]})

            # Если товар уже существует, увеличиваем счетчик обновлений
            if existing_product:
                updated_count += 1
            else:
                # Если товар новый, увеличиваем счетчик добавлений
                inserted_count += 1

            bulk_operations.append(
                ReplaceOne(
                    {"_id": product["_id"]},
                    product,
                    upsert=True
                )
            )

            # Добавляем товар в общую коллекцию
            product_copy = product.copy()
            product_copy["_id"] = f"{parser_name}_{product['url']}"
            product_copy["source"] = parser_name

            bulk_operations_all.append(
                ReplaceOne(
                    {"_id": product_copy["_id"]},
                    product_copy,
                    upsert=True
                )
            )

        try:
            # Выполняем массовую запись для коллекции парсера
            if bulk_operations:
                result = collection.bulk_write(bulk_operations)
                print(f"[{parser_name}] Успешно обновлено {updated_count} / {len(bulk_operations)} товаров. Добавлено {inserted_count} новых товаров.")

            # Очищаем старые товары для данного парсера в общей коллекции
            if bulk_operations_all:
                all_products_collection.delete_many({"source": parser_name})  # Удаляем все товары от конкретного парсера
                result_all = all_products_collection.bulk_write(bulk_operations_all)
                print(f"[all_products] Добавлено/обновлено {inserted_count + updated_count} / {len(bulk_operations_all)} товаров из {parser_name}.")

        except BulkWriteError as e:
            print(f"[{parser_name}] Ошибка массовой записи: {e.details}")

        return True
