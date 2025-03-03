from models.btu_request_model import BTURequestModel
from models.btu_response_model import BTUResponseModel


def calculate_btu(request: BTURequestModel) -> BTUResponseModel:
    # Преобразование единиц измерения
    room_size_m2 = request.room_size * 0.092903 if request.size_unit.lower() == 'square feet' else request.room_size
    ceiling_height_m = request.ceiling_height * 0.3048 if request.height_unit.lower() == 'feet' else request.ceiling_height

    # Коэффициенты и базовые нагрузки
    sun_exposure_coefficient = {'low': 30, 'medium': 35, 'high': 40}.get(request.sun_exposure.lower(), 35)
    Q1 = room_size_m2 * ceiling_height_m * sun_exposure_coefficient / 1000
    Q2 = request.people_count * 0.1
    Q3 = request.number_of_computers * 0.3 + request.number_of_tvs * 0.2 + request.other_appliances_kwattage * 0.3

    # Учет вентиляции
    if request.has_ventilation and 0.5 <= request.air_exchange_rate <= 3.0:
        ventilation_increase_map = {0.5: 0.11, 1.0: 0.22, 1.5: 0.33, 2.0: 0.44, 2.5: 0.55, 3.0: 0.66}
        ventilation_increase = ventilation_increase_map.get(request.air_exchange_rate, 0)
        Q1 *= 1 + ventilation_increase

    # Суммарная мощность
    Q = Q1 + Q2 + Q3

    # Дополнительные факторы
    if request.guaranteed_20_degrees:
        Q *= 1.15
    if request.is_top_floor:
        Q *= 1.15
    if request.has_large_window and request.window_area > 2.0:
        window_load_map = {'low': 0.05, 'medium': 0.1, 'high': 0.2}
        window_additional_load = window_load_map.get(request.sun_exposure.lower(), 0)
        Q += (request.window_area - 2.0) * window_additional_load

    # Рекомендованный диапазон
    lower_limit = Q * 0.95
    upper_limit = Q * 1.15

    # Перевод в BTU
    kw_to_btu = 3412
    Q_BTU = round(Q * kw_to_btu / 1000) * 1000
    lower_limit_BTU = round(lower_limit * kw_to_btu / 1000) * 1000
    upper_limit_BTU = round(upper_limit * kw_to_btu / 1000) * 1000

    # Ограничение максимального значения BTU до 300000
    Q_BTU = min(Q_BTU, 300000)
    lower_limit_BTU = min(lower_limit_BTU, 300000)
    upper_limit_BTU = min(upper_limit_BTU, 300000)

    # Возвращаем Pydantic-модель
    return BTUResponseModel(
        calculated_power_kw=round(Q, 2),
        calculated_power_btu=Q_BTU,
        recommended_range_kw={"lower": round(lower_limit, 2), "upper": round(upper_limit, 2)},
        recommended_range_btu={"lower": lower_limit_BTU, "upper": upper_limit_BTU}
    )
