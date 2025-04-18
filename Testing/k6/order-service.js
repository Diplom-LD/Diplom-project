import http from 'k6/http';
import { sleep, check } from 'k6';

export let options = {
    vus: 50,
    duration: '2m',
};

const AUTH_URL = 'http://auth-service:8080';
const ORDER_URL = 'http://order-service:8080';

export default function () {
    // 🔐 Авторизация менеджера
    const loginPayload = JSON.stringify({
        identifier: 'manager1',
        password: 'Test@123'
    });

    const loginRes = http.post(`${AUTH_URL}/auth/sign-in`, loginPayload, {
        headers: { 'Content-Type': 'application/json' },
    });

    check(loginRes, {
        '🔐 Логин успешен (200)': (res) => res.status === 200,
        '🔐 accessToken получен': (res) => res.json('accessToken') !== undefined,
    });

    const token = loginRes.json('accessToken');
    const authHeaders = {
        headers: {
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json',
        },
    };

    sleep(0.3);

    // 📥 Получение всех заявок
    const allOrdersRes = http.get(`${ORDER_URL}/manager/orders/get/all`, authHeaders);
    check(allOrdersRes, {
        '📥 Все заявки (200)': (res) => res.status === 200,
    });

    let orders = [];
    try {
        orders = allOrdersRes.json();
    } catch (e) {
        // fallback: leave orders empty
    }

    sleep(0.3);

    // 🔎 Получение заявки по ID (если есть)
    if (orders.length > 0) {
        const randomOrder = orders[Math.floor(Math.random() * orders.length)];
        const orderId = randomOrder.id || randomOrder.orderId || randomOrder.Id;

        const orderRes = http.get(`${ORDER_URL}/manager/orders/get/${orderId}`, authHeaders);
        check(orderRes, {
            '🔎 Заявка по ID (200)': (res) => res.status === 200,
        });

        sleep(0.3);
    }

    // 💤 Пауза между итерациями
    sleep(1);
}
