import http from 'k6/http';
import { sleep, check } from 'k6';

export let options = {
  vus: 30, // виртуальных пользователей
  duration: '1m', // время теста
};

const BASE_URL = 'http://btu-calc-service:8000'; // имя из docker-compose (если изнутри Docker)
const endpoint = '/BTUCalcService/calculate_btu';

export default function () {
  const payload = JSON.stringify({
    room_size: 20,
    size_unit: "square meters",
    ceiling_height: 2.75,
    height_unit: "meters",
    sun_exposure: "medium",
    people_count: 1,
    number_of_computers: 1,
    number_of_tvs: 0,
    other_appliances_kwattage: 0,
    has_ventilation: false,
    air_exchange_rate: 0,
    guaranteed_20_degrees: false,
    is_top_floor: false,
    has_large_window: false,
    window_area: 2.5
  });

  const headers = {
    headers: { 'Content-Type': 'application/json' },
  };

  const res = http.post(`${BASE_URL}${endpoint}`, payload, headers);

  check(res, {
    '✅ Статус 200': (r) => r.status === 200,
    '✅ Ответ содержит BTU': (r) => r.json('btu_result') !== undefined,
  });

  sleep(1);
}
