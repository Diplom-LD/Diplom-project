from pydantic import BaseModel, Field
from typing import Optional


class AirConditionerModel(BaseModel):
    name: str = Field(..., description="Название кондиционера")
    url: str = Field(..., description="URL на товар")
    price: Optional[float] = Field(None, description="Цена товара")
    currency: str = Field(..., description="Валюта")
    btu: Optional[int] = Field(None, description="BTU (мощность)")
    service_area: Optional[float] = Field(None, description="Площадь обслуживания в м²")
    store: str = Field(..., description="Название магазина")

    class Config:
        from_attributes = True
