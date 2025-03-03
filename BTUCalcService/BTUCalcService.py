from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from starlette.websockets import WebSocketState
from apscheduler.schedulers.background import BackgroundScheduler
from apscheduler.triggers.cron import CronTrigger
from fastapi.responses import RedirectResponse, HTMLResponse
from datetime import datetime, timedelta
from zoneinfo import ZoneInfo
from contextlib import asynccontextmanager
import asyncio
import logging
from parsers.conditionereParser import ConditionereParser
from parsers.eurosantehParser import EurosantehParser
from parsers.greeParser import GreeParser
from parsers.jaraParser import JaraParser
from parsers.termoformatParser import TermoformatParser
from parsers.termocontrolParser import TermoControlParser
from services.db import get_mongo_client
from services.mongodb_saver import MongoDBParserSaver
from routers.products_router import router as products_router
from routers.btu_router import router as btu_router

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")

chisinau_tz = ZoneInfo("Europe/Chisinau")

status_message = "Ожидаем запуск парсеров..."
is_running = False
loop = None
scheduler_started = False  

def get_local_time():
    return datetime.now(chisinau_tz)

async def run_parser(parser_class, parser_name):
    global status_message
    try:
        status_message = f"Парсинг {parser_name} начался..."
        parser = parser_class()
        products = await parser.run()

        if products:
            db = get_mongo_client()
            saver = MongoDBParserSaver(db)
            saver.save_products(parser_name, products)
            status_message = f"Парсинг {parser_name} завершён!"
        else:
            status_message = f"{parser_name}: Нет новых данных."
    except Exception as e:
        status_message = f"Ошибка при парсинге {parser_name}: {e}"
        logging.error(f"[{parser_name}] Парсер упал с ошибкой: {e}", exc_info=True)
    finally:
        logging.info(status_message)

async def run_all_parsers():
    global is_running, status_message
    if is_running:
        logging.warning("Парсеры уже работают, пропускаем запуск.")
        return

    is_running = True
    status_message = f"Запуск всех парсеров в {get_local_time().strftime('%Y-%m-%d %H:%M:%S')}"
    logging.info(status_message)

    tasks = [
        run_parser(ConditionereParser, "conditionere"),
        run_parser(EurosantehParser, "eurosanteh"),
        run_parser(GreeParser, "gree"),
        run_parser(JaraParser, "jara"),
        run_parser(TermoformatParser, "termoformat"),
        run_parser(TermoControlParser, "termocontrol"),
    ]

    await asyncio.gather(*tasks)

    is_running = False
    status_message = f"Парсеры завершили работу в {get_local_time().strftime('%Y-%m-%d %H:%M:%S')}"
    logging.info(status_message)

async def check_database():
    """Проверяем наличие всех коллекций перед запуском"""
    db = get_mongo_client()
    existing_collections = set(db.list_collection_names())

    required_collections = {
        "conditionere_products",
        "eurosanteh_products",
        "gree_products",
        "jara_products",
        "termoformat_products",
        "termocontrol_products",
        "all_products"
    }

    missing_collections = required_collections - existing_collections

    if missing_collections:
        logging.info(f"Отсутствуют коллекции: {missing_collections}, начинаем первичный парсинг")
        asyncio.create_task(run_all_parsers())
    else:
        logging.info("Все коллекции уже есть, первичный парсинг не требуется")

@asynccontextmanager
async def lifespan(app: FastAPI):
    global loop, scheduler_started
    loop = asyncio.get_running_loop()
    logging.info("FastAPI запущен вместе с планировщиком парсеров")

    for _ in range(5):
        try:
            db = get_mongo_client()
            db.command("ping")
            logging.info("Подключение к базе данных успешно")
            break
        except Exception as e:
            logging.error(f"Не удалось подключиться к базе данных: {e}")
            await asyncio.sleep(5)

    await check_database()

    if not scheduler_started:
        logging.info("Запуск APScheduler...")
        scheduler.add_job(
            lambda: asyncio.run_coroutine_threadsafe(run_all_parsers(), loop),
            CronTrigger(minute=0, timezone="Europe/Chisinau"),
            max_instances=1
        )
        scheduler.start()
        scheduler_started = True
        logging.info("APScheduler запущен")

    yield

app = FastAPI(lifespan=lifespan)

scheduler = BackgroundScheduler(timezone="Europe/Chisinau")

app.include_router(products_router)
app.include_router(btu_router) 

@app.get("/", include_in_schema=False)
async def root():
    return RedirectResponse(url="/BTUCalcService/schedule-page")

@app.get("/BTUCalcService/schedule-page", response_class=HTMLResponse)
async def get_parser_schedule_page():
    now = get_local_time()
    next_trigger = now.replace(minute=0, second=0, microsecond=0) + timedelta(hours=1)

    return f"""
    <!DOCTYPE html>
    <html lang="ru">
    <head>
        <meta charset="UTF-8">
        <title>Статус планировщика парсеров</title>
        <style>
            body {{ font-family: Arial, sans-serif; text-align: center; margin-top: 50px; }}
            .timer {{ font-size: 2rem; color: green; }}
        </style>
    </head>
    <body>
        <h1>Статус планировщика парсеров</h1>
        <p>Текущее время сервера: <span id="current-time">{now.strftime('%Y-%m-%d %H:%M:%S')}</span></p>
        <p>Следующий запуск парсеров: <strong id="next-trigger">{next_trigger.strftime('%Y-%m-%d %H:%M:%S')}</strong></p>
        <p>Статус парсера:</p>
        <div class="timer" id="status">Ожидаем данные...</div>

        <script>
            function formatDate(date) {{
                const pad = (num) => num.toString().padStart(2, "0");
                return `${{date.getFullYear()}}-${{pad(date.getMonth() + 1)}}-${{pad(date.getDate())}} ` +
                       `${{pad(date.getHours())}}:${{pad(date.getMinutes())}}:${{pad(date.getSeconds())}}`;
            }}

            function updateCurrentTime() {{
                const now = new Date(new Date().toLocaleString("en-US", {{ timeZone: "Europe/Chisinau" }}));
                document.getElementById('current-time').innerText = formatDate(now);
            }}

            setInterval(updateCurrentTime, 1000);

            const socket = new WebSocket("ws://" + window.location.host + "/BTUCalcService/ws-status");

            socket.onmessage = function(event) {{
                const statusElement = document.getElementById("status");
                statusElement.innerText = event.data;
                statusElement.style.color = event.data.includes("Ошибка") ? "red" : "green";

                if (event.data.includes("Парсеры завершили работу в")) {{
                    // Обновляем следующий запуск
                    const nextTriggerTime = new Date();
                    nextTriggerTime.setHours(nextTriggerTime.getHours() + 1, 0, 0, 0);
                    document.getElementById("next-trigger").innerText = formatDate(nextTriggerTime);
                }}
            }};

            socket.onopen = function() {{
                console.log("WebSocket подключен.");
            }};

            socket.onerror = function(error) {{
                console.error("Ошибка WebSocket:", error);
            }};

            socket.onclose = function(event) {{
                console.warn("WebSocket закрыт. Переподключение через 5 секунд...");
                setTimeout(() => {{
                    location.reload();
                }}, 5000);
            }};
        </script>
    </body>
    </html>
    """

@app.websocket("/BTUCalcService/ws-status")
async def websocket_status(websocket: WebSocket):
    """ WebSocket для обновления статуса парсеров в реальном времени """
    await websocket.accept()
    try:
        while True:
            if websocket.client_state == WebSocketState.DISCONNECTED:
                logging.warning("Клиент отключился от WebSocket.")
                break  

            await websocket.send_text(status_message)
            await asyncio.sleep(2)
    except WebSocketDisconnect:
        logging.warning("Клиент отключился от WebSocket.")
    except RuntimeError as e:
        if "Cannot call 'send' once a close message has been sent" in str(e):
            logging.warning("Попытка отправить данные в закрытый WebSocket.")
        else:
            logging.error(f"Ошибка WebSocket: {e}", exc_info=True)
    except Exception as e:
        logging.error(f"Неизвестная ошибка WebSocket: {e}", exc_info=True)
    finally:
        logging.info("WebSocket-соединение закрыто.")
        try:
            await websocket.close()
        except Exception:
            pass  

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("BTUCalcService:app", host="0.0.0.0", port=8000)
