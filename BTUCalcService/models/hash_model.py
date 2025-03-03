from pydantic import BaseModel, Field
from datetime import datetime
from typing import Optional

class HashModel(BaseModel):
    store: str = Field(..., description="Название магазина")
    hash: str = Field(..., description="Хэш сумма данных")
    updated_at: Optional[datetime] = Field(None, description="Время последнего обновления")

    class Config:
        from_attributes = True
