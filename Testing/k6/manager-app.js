import http from 'k6/http';
import { sleep, check } from 'k6';

export let options = {
    vus: 50,              // Кол-во виртуальных пользователей
    duration: '2m',       // Длительность теста
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
        '🔐 Логин успешен (200)': (res) => res.status === 200,
        '🔐 accessToken получен': (res) => res.json('accessToken') !== undefined,
    });

    const token = loginRes.json('accessToken');

    const authHeaders = {
        headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json'
        }
    };

    sleep(0.2);

    for (let i = 0; i < 5; i++) {
        const profileRes = http.get(`${BASE_URL}/auth/account/my-profile`, authHeaders);
        check(profileRes, {
            [`${i + 1}. ✅ Профиль загружен (200)`]: (res) => res.status === 200,
        });

        sleep(0.2);

        const unauthRes = http.get(`${BASE_URL}/auth/account/my-profile`);
        check(unauthRes, {
            [`${i + 1}. ❌ Без токена отказ (401)`]: (res) => res.status === 401,
        });

        sleep(0.2);

        const clientsRes = http.get(`${BASE_URL}/auth/account/get-all-clients`, authHeaders);
        check(clientsRes, {
            [`${i + 1}. 📋 Клиенты получены (200)`]: (res) => res.status === 200,
        });

        sleep(0.2);

        const getClientRes = http.get(`${BASE_URL}/auth/account/get-profile/client1`, authHeaders);
        check(getClientRes, {
            [`${i + 1}. 🔎 Поиск клиента`]: (res) => res.status === 200 || res.status === 404,
        });

        sleep(0.2);
    }
}
