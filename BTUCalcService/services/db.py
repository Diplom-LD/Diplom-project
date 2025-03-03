import os
from pymongo import MongoClient

MONGO_URL = os.getenv("MONGO_URL", "mongodb://localhost:27017")

def get_mongo_client():
    client = MongoClient(MONGO_URL)
    return client["btu_database"]
