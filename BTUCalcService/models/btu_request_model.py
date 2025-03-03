from pydantic import BaseModel, Field, model_validator
from typing import Optional

class BTURequestModel(BaseModel):
    room_size: float = Field(..., gt=0, le=500, description="Размер комнаты (1-500 м²)")
    size_unit: str = Field(..., description="Единица измерения: square meters / square feet")
    ceiling_height: float = Field(..., ge=2, le=10, description="Высота потолка (2-10 м)")
    height_unit: str = Field(..., description="Единица измерения: meters / feet")
    sun_exposure: str = Field(..., pattern="^(low|medium|high)$", description="low / medium / high")
    people_count: int = Field(..., ge=1, le=100, description="Количество людей (1-100)")
    number_of_computers: int = Field(..., ge=0, le=50, description="Количество компьютеров (0-50)")
    number_of_tvs: int = Field(..., ge=0, le=50, description="Количество телевизоров (0-50)")
    other_appliances_kwattage: float = Field(..., ge=0, le=20, description="Мощность других приборов (0-20 кВт)")
    has_ventilation: bool = Field(..., description="Есть ли вентиляция")
    air_exchange_rate: Optional[float] = Field(None, ge=0, le=3, description="Кратность воздухообмена (0-3)")
    guaranteed_20_degrees: bool = Field(..., description="Гарантированные 20 градусов?")
    is_top_floor: bool = Field(..., description="Последний этаж?")
    has_large_window: bool = Field(..., description="Большая площадь остекления?")
    window_area: Optional[float] = Field(None, ge=0, le=100, description="Площадь окон (0-100 м²)")

    @model_validator(mode="before")
    def validate_logic(cls, values):
        if values.get("has_ventilation", False):
            if values.get("air_exchange_rate") is None or values["air_exchange_rate"] < 0.5:
                raise ValueError("Если вентиляция включена, нужно указать кратность воздухообмена (0.5-3).")
        else:
            values["air_exchange_rate"] = 0 

        if values.get("has_large_window"):
            if values.get("window_area") is None:
                raise ValueError("Если есть большие окна, нужно указать площадь окон (0-100 м²).")

        return values
