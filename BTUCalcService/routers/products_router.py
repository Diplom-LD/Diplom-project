from fastapi import APIRouter, HTTPException, Query
from services.db import get_mongo_client
import logging

router = APIRouter(prefix="/BTUCalcService/products", tags=["Products"])

db = get_mongo_client()

logger = logging.getLogger(__name__)

# Функция для преобразования строки в число (если возможно)
def parse_btu(value):
    try:
        return int(value)
    except (ValueError, TypeError):
        return value  # Если не удается преобразовать, оставляем как строку

@router.get("/range/")
async def get_products_by_btu_range(
    btu_min: int = Query(..., description="Минимальное значение BTU"),
    btu_max: int = Query(..., description="Максимальное значение BTU"),
):
    """Получить кондиционеры по диапазону BTU из общей коллекции."""
    btu_min = parse_btu(btu_min)
    btu_max = parse_btu(btu_max)

    if isinstance(btu_min, int) and isinstance(btu_max, int) and btu_min > btu_max:
        raise HTTPException(
            status_code=400,
            detail="Минимальное значение BTU не может быть больше максимального",
        )

    collection = db["all_products"]

    products = list(
        collection.find(
            {"btu": {"$gte": btu_min, "$lte": btu_max, "$ne": None}},
            {"_id": 0},
        )
    )

    if not products:
        raise HTTPException(status_code=404, detail="Товары не найдены")

    return products

@router.get("/btu/{btu}")
async def get_products_by_exact_btu(btu: str):
    """Получить кондиционеры по конкретному BTU из общей коллекции."""
    btu = parse_btu(btu)

    collection = db["all_products"]

    products = list(
        collection.find(
            {"btu": btu},
            {"_id": 0},
        )
    )

    if not products:
        raise HTTPException(status_code=404, detail="Товары не найдены")

    return products

@router.get("/extremes/")
async def get_extreme_btu_products():
    """Получить кондиционеры с минимальным и максимальным BTU."""
    collection = db["all_products"]

    extreme_values = collection.aggregate([
        {
            "$project": {
                "btu": {
                    "$toInt": {
                        "$arrayElemAt": [
                            {"$split": [{"$toString": "$btu"}, " "]},
                            0
                        ]
                    }
                },
                "name": 1, "price": 1, "currency": 1, "service_area": 1,
                "store": 1, "url": 1
            }
        },
        {
            "$group": {
                "_id": None,
                "min_btu": {"$min": "$btu"},
                "max_btu": {"$max": "$btu"}
            }
        }
    ])

    extremes = next(extreme_values, None)
    if not extremes or extremes["min_btu"] is None or extremes["max_btu"] is None:
        logger.error("❌ Не удалось определить диапазоны BTU")
        raise HTTPException(status_code=404, detail="Не удалось определить диапазоны BTU")

    btu_min = extremes["min_btu"]
    btu_max = extremes["max_btu"]

    logger.info(f"🔍 Найден диапазон BTU: min={btu_min}, max={btu_max}")

    products = list(collection.aggregate([
        {
            "$project": {
                "btu": {
                    "$toInt": {
                        "$arrayElemAt": [
                            {"$split": [{"$toString": "$btu"}, " "]},
                            0
                        ]
                    }
                },
                "name": 1, "price": 1, "currency": 1, "service_area": 1,
                "store": 1, "url": 1
            }
        },
        {"$match": {"btu": {"$in": [btu_min, btu_max]}}}
    ]))

    if not products:
        logger.warning("⚠️ Товары с крайними BTU не найдены")
        raise HTTPException(status_code=404, detail="Товары с крайними BTU не найдены")

    logger.info(f"✅ Найдено {len(products)} товаров с крайними значениями BTU")
    for product in products:
        logger.info(f"📌 {product}")

    return {
        "btu_min": btu_min,
        "btu_max": btu_max,
        "products": products
    }


@router.get("/stores/")
async def get_stores():
    """Получить список магазинов с кондиционерами."""
    stores = db.list_collection_names()
    stores = [store.replace("_products", "") for store in stores if store.endswith("_products")]
    
    if not stores:
        raise HTTPException(status_code=404, detail="Магазины не найдены")
    
    return {"stores": stores}

@router.get("/store/{store_name}")
async def get_products_by_store(store_name: str):
    """Получить кондиционеры из конкретного магазина."""
    collection_name = f"{store_name.lower()}_products"
    if collection_name not in db.list_collection_names():
        raise HTTPException(status_code=404, detail=f"Магазин {store_name} не найден")

    products = list(
        db[collection_name].find(
            {"_id": {"$ne": "metadata"}},
            {"_id": 0},
        )
    )

    if not products:
        raise HTTPException(status_code=404, detail="Товары не найдены")

    return products


@router.get("/service_area/{area}")
async def get_products_by_service_area(area: int):
    """Получить кондиционеры по точной площади обслуживания или в диапазоне ±5 м²."""
    collection = db["all_products"]
    
    # Ищем кондиционеры в диапазоне ±5 м²
    products = list(
        collection.find(
            {"service_area": {"$gte": area - 5, "$lte": area + 5}},  # Диапазон для гибкого поиска
            {"_id": 0},
        )
    )
    
    if not products:
        raise HTTPException(status_code=404, detail="Товары не найдены")
    
    return products

@router.get("/price/{price}")
async def get_products_by_exact_price(price: int):
    """Получить кондиционеры по конкретной цене."""
    collection = db["all_products"]
    
    products = list(
        collection.find(
            {"price": price},
            {"_id": 0},
        )
    )
    
    if not products:
        raise HTTPException(status_code=404, detail="Товары не найдены")
    
    return products

@router.get("/price/")
async def get_products_by_price_range(
    price_min: int = Query(..., description="Минимальная цена"),
    price_max: int = Query(..., description="Максимальная цена")
):
    """Получить кондиционеры по диапазону цен."""
    if price_min > price_max:
        raise HTTPException(status_code=400, detail="Минимальная цена не может быть больше максимальной")
    
    collection = db["all_products"]
    
    products = list(
        collection.find(
            {"price": {"$gte": price_min, "$lte": price_max}},
            {"_id": 0},
        )
    )
    
    if not products:
        raise HTTPException(status_code=404, detail="Товары не найдены")
    
    return products