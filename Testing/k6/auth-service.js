import http from 'k6/http';
import { sleep, check } from 'k6';

export let options = {
    vus: 50,              // больше пользователей
    duration: '2m',       // 2 минуты
};

const BASE_URL = 'http://auth-service:8080';

export default function () {
    const loginPayload = JSON.stringify({
        identifier: 'manager1',
        password: 'Test@123'
    });

    const loginHeaders = {
        headers: {
            'Content-Type': 'application/json'
        }
    };

    const loginRes = http.post(`${BASE_URL}/auth/sign-in`, loginPayload, loginHeaders);

    check(loginRes, {
        'Логин успешен (200)': (res) => res.status === 200,
        'accessToken присутствует': (res) => res.json('accessToken') !== undefined,
    });

    const token = loginRes.json('accessToken');

    const authHeaders = {
        headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    };

    sleep(0.2);

    // 🔁 Повторим больше запросов для нагрева
    for (let i = 0; i < 5; i++) {
        // Профиль
        const profileRes = http.get(`${BASE_URL}/auth/account/my-profile`, authHeaders);
        check(profileRes, {
            [`${i + 1}. Профиль (200)`]: (res) => res.status === 200,
        });

        sleep(0.2);

        // Без токена
        const unauthRes = http.get(`${BASE_URL}/auth/account/my-profile`);
        check(unauthRes, {
            [`${i + 1}. Без токена отказ (401)`]: (res) => res.status === 401,
        });

        sleep(0.2);

        // Клиенты
        const clientsRes = http.get(`${BASE_URL}/auth/account/get-all-clients`, authHeaders);
        check(clientsRes, {
            [`${i + 1}. Клиенты (200)`]: (res) => res.status === 200,
        });

        sleep(0.2);

        // Получение профиля по логину
        const getClientRes = http.get(`${BASE_URL}/auth/account/get-profile/client1`, authHeaders);
        check(getClientRes, {
            [`${i + 1}. Поиск клиента`]: (res) => res.status === 200 || res.status === 404,
        });

        sleep(0.2);
    }
}
