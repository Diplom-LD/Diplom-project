import json
from fastapi import APIRouter, HTTPException, Request
from models.btu_request_model import BTURequestModel
from models.btu_response_model import BTUResponseModel
from services.btu_calculator import calculate_btu

router = APIRouter()

@router.post(
    "/BTUCalcService/calculate_btu",
    response_model=BTUResponseModel,
    summary="Рассчитать мощность BTU",
    description="Рассчитывает необходимую мощность кондиционера (в BTU и кВт) на основе параметров комнаты."
)
async def calculate_btu_route(request: Request):
    """Рассчитать мощность BTU на основе параметров комнаты."""
    try:
        body = await request.json() 
        print("📥 Получены входные данные:", json.dumps(body, indent=2, ensure_ascii=False))  

        validated_request = BTURequestModel(**body)  
        result = calculate_btu(validated_request)
        return result
    except Exception as e:
        print("Ошибка при обработке запроса:", str(e)) 
        raise HTTPException(status_code=400, detail=f"Ошибка при расчёте BTU: {str(e)}")
