from pydantic import BaseModel

class PowerRangeModel(BaseModel):
    lower: float
    upper: float

class BTUResponseModel(BaseModel):
    calculated_power_kw: float
    calculated_power_btu: int
    recommended_range_kw: PowerRangeModel
    recommended_range_btu: PowerRangeModel

    class Config:
        from_attributes = True
