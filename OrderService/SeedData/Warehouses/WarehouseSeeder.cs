using MongoDB.Driver;
using OrderService.Data.Warehouses;
using OrderService.Models.Warehouses;
using OrderService.Services.GeoLocation.GeoCodingClient;

namespace OrderService.SeedData.Warehouses
{
    public class WarehouseSeeder(WarehouseMongoContext context, ILogger<WarehouseSeeder> logger)
    {
        private readonly IMongoCollection<Warehouse> _warehouseCollection = context.Warehouses;
        private readonly IMongoCollection<EquipmentStock> _equipmentCollection = context.EquipmentStock;
        private readonly IMongoCollection<MaterialsStock> _materialsCollection = context.MaterialsStock;
        private readonly IMongoCollection<ToolsStock> _toolsCollection = context.ToolsStock;
        private readonly ILogger<WarehouseSeeder> _logger = logger;

        public async Task SeedAsync(IServiceProvider services)
        {
            _logger.LogInformation("🔹 Начинаем сеединг данных...");

            if (!await IsDataSeeded())
            {
                _logger.LogInformation("📦 Seeding складов...");
                await SeedWarehouses(services);

                var warehouseIds = await GetWarehouseIds();
                _logger.LogInformation("📦 Получены {Count} складов для наполнения данными.", warehouseIds.Count);

                _logger.LogInformation("📦 Заполняем оборудование...");
                await SeedEquipmentStock(warehouseIds);

                _logger.LogInformation("📦 Заполняем материалы...");
                await SeedMaterialsStock(warehouseIds);

                _logger.LogInformation("📦 Заполняем инструменты...");
                await SeedToolsStock(warehouseIds);

                _logger.LogInformation("✅ Seeding завершён!");
            }
            else
            {
                _logger.LogWarning("⚠️ Данные уже существуют. Пропускаем сеединг.");
            }
        }

        private async Task<bool> IsDataSeeded()
        {
            var warehouseCount = await _warehouseCollection.CountDocumentsAsync(FilterDefinition<Warehouse>.Empty);
            return warehouseCount > 0;
        }

        private async Task SeedWarehouses(IServiceProvider services)
        {
            if (await _warehouseCollection.CountDocumentsAsync(FilterDefinition<Warehouse>.Empty) > 0)
                return;

            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var geoCodingService = new GeoCodingService(new HttpClient(), loggerFactory.CreateLogger<GeoCodingService>());

            var warehouses = new List<Warehouse>
            {
                await CreateWarehouseAsync("Склад 1", "10, улица Кантонулуй", geoCodingService),
                await CreateWarehouseAsync("Склад 2", "17, Заводская улица, Отоваска", geoCodingService),
                await CreateWarehouseAsync("Склад 3", "ул. Унирий, 20/2, Ставчены, Кишинёв", geoCodingService),
                await CreateWarehouseAsync("Склад 4", "25/9, проспект Куза Водэ", geoCodingService),
                await CreateWarehouseAsync("Склад 5", "1A, 2-й переулок Вовинцень, Dumbrava", geoCodingService),
                await CreateWarehouseAsync("Склад 6", "переулок Кэлэторилор, Кишинёв, Сектор Чеканы", geoCodingService),
                await CreateWarehouseAsync("Склад 7", "22, улица Тома Чорбэ, Кишинёв", geoCodingService),
                await CreateWarehouseAsync("Склад 8", "82, проспект Дечебал, Ботаника", geoCodingService),
                await CreateWarehouseAsync("Склад 9", "162, улица Колумна, Вистерничены, Кишинёв", geoCodingService),
                await CreateWarehouseAsync("Склад 10", "улица Матея Басараба, Верхняя Рышкановка, Кишинёв", geoCodingService)
            };

            await _warehouseCollection.InsertManyAsync(warehouses);
        }

        private async Task<Warehouse> CreateWarehouseAsync(string name, string address, GeoCodingService geoCodingService)
        {
            _logger.LogInformation("📍 Запрос геокодинга для адреса: {Address}", address);

            try
            {
                var (Latitude, Longitude, DisplayName) = await geoCodingService.GetBestCoordinateAsync(address) ?? (Latitude: 0.0, Longitude: 0.0, DisplayName: "Unknown");

                _logger.LogInformation("✅ Координаты найдены: {Latitude}, {Longitude} для {Address}", Latitude, Longitude, address);

                return new Warehouse
                {
                    ID = Guid.NewGuid(),
                    Name = name,
                    Address = DisplayName == "Unknown" ? "Неизвестный адрес" : address,
                    Latitude = Latitude,
                    Longitude = Longitude
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при геокодировании адреса {Address}", address);
                return new Warehouse
                {
                    ID = Guid.NewGuid(),
                    Name = name,
                    Address = "Ошибка геолокации",
                    Latitude = 0,
                    Longitude = 0
                };
            }
        }

        private async Task<List<string>> GetWarehouseIds()
        {
            return await _warehouseCollection.Find(FilterDefinition<Warehouse>.Empty)
                                             .Project(w => w.ID.ToString()) 
                                             .ToListAsync();
        }

        private async Task SeedEquipmentStock(List<string> warehouseIds)
        {
            if (warehouseIds.Count == 0 || await _equipmentCollection.EstimatedDocumentCountAsync() > 0)
                return;

            var models = new List<string>
            {
                "LG Standard Plus", "Samsung AR9500", "Daikin FTXB25C",
                "Mitsubishi MSZ-HJ25VA", "Haier HSU-12H", "Gree GWH12KF",
                "Electrolux EACS-12H", "Toshiba RAS-10N3KVR", "Panasonic CS-E12",
                "Hitachi RAS-S10", "Carrier 42QHA012DS", "Fujitsu ASYG12KMCC",
                "York YHJF24S41S1", "Trane XR16", "Rheem RA16", "Amana ASX16",
                "Goodman GSX16", "Lennox XC25", "Bosch Climate 5000",
                "Whirlpool WHAC-5012", "Sharp AH-XP18WHT", "Hisense AS-18UR4SVDTEG05",
                "Sanyo SAP-KRV123GHS", "Daewoo DSB-F1281ELH-V", "Kenmore 77087",
                "Blue Star 5HW18ZARTX", "Voltas 185V JZJ", "O General ASGA18FTTA",
                "Godrej GSC 18 FGU 7 RWPH", "Videocon VSN55.WV1-MDA",
                "IFB IACS18KA3TC", "Sansui SNA55.WS1-MDA", "Midea MAW12V1QWT",
                "TCL TAC-18CHSD", "Micromax ACS18ED5AS01WHI", "Hitachi RAS-E13CY",
                "Haier HSU-19CXAS3N", "LG Dual Inverter", "Samsung Wind-Free",
                "Daikin FTKM50TV", "Mitsubishi Heavy Duty", "Hitachi Kaze Plus",
                "Blue Star 5CNHW18PAFU", "Voltas 183VCZT", "O General ASGG18CPTA",
                "Godrej GIC 18YTC3-WTA", "Videocon VSN55.WV1-MDA", "IFB IACS18AK3TC",
                "Sansui SNA55.WS1-MDA", "Midea MAW12V1QWT", "TCL TAC-18CHSD",
                "Micromax ACS18ED5AS01WHI", "Hitachi RAS-E13CY", "Haier HSU-19CXAS3N"
            };

            var random = new Random();
            var equipmentStock = warehouseIds.SelectMany(warehouseId =>
                models.Select(model => new EquipmentStock
                {
                    ID = Guid.NewGuid(),
                    WarehouseId = Guid.Parse(warehouseId),
                    ModelName = model,
                    BTU = random.Next(9000, 30000),
                    ServiceArea = random.Next(20, 100),
                    Price = random.Next(45000, 95000),
                    Quantity = random.Next(1, 5)
                })).ToList();

            await _equipmentCollection.InsertManyAsync(equipmentStock);
        }

        private async Task SeedMaterialsStock(List<string> warehouseIds)
        {
            if (warehouseIds.Count == 0 || await _materialsCollection.CountDocumentsAsync(FilterDefinition<MaterialsStock>.Empty) > 0)
                return;

            var materials = new List<string>
            {
                "Медная трубка 1/4 дюйма", "Медная трубка 3/8 дюйма", "Медная трубка 1/2 дюйма",
                "Фреон R410A", "Фреон R32", "Фреон R22",
                "Крепежные анкера", "Крепежные болты", "Крепежные гайки",
                "Теплоизоляция для труб 1/4 дюйма", "Теплоизоляция для труб 3/8 дюйма", "Теплоизоляция для труб 1/2 дюйма",
                "Дренажный шланг 1/2 дюйма", "Дренажный шланг 3/4 дюйма", "Дренажный шланг 1 дюйм",
                "Гофрированная труба 1/2 дюйма", "Гофрированная труба 3/4 дюйма", "Гофрированная труба 1 дюйм",
                "Монтажный профиль 1 метр", "Монтажный профиль 2 метра", "Монтажный профиль 3 метра",
                "Фильтр для кондиционера", "Фильтр для очистки воздуха", "Фильтр угольный",
                "Кабель для подключения 2x1.5 мм", "Кабель для подключения 3x1.5 мм", "Кабель для подключения 4x1.5 мм",
                "Антикоррозийное покрытие", "Герметик для соединений", "Монтажная пена",
                "Изоляционная лента", "Клейкая лента", "Зажимы для труб",
                "Кронштейны для наружного блока", "Кронштейны для внутреннего блока",
                "Переходники для труб", "Соединительные муфты", "Угловые фитинги",
                "Сетевой фильтр", "Стабилизатор напряжения", "Удлинитель",
                "Защитный кожух для кондиционера", "Декоративные короба для труб",
                "Кабельные стяжки", "Кабельные каналы"
            };

            var random = new Random();
            var materialsStock = warehouseIds.SelectMany(warehouseId =>
                materials.Select(material => new MaterialsStock
                {
                    ID = Guid.NewGuid(),
                    WarehouseId = Guid.Parse(warehouseId),
                    MaterialName = material,
                    Quantity = random.Next(10, 200),
                    Price = random.Next(100, 5000)
                })).ToList();

            await _materialsCollection.InsertManyAsync(materialsStock);
        }

        private async Task SeedToolsStock(List<string> warehouseIds)
        {
            if (warehouseIds.Count == 0 || await _toolsCollection.CountDocumentsAsync(FilterDefinition<ToolsStock>.Empty) > 0)
                return;

            var tools = new List<string>
            {
                "Вакуумный насос", "Манометрический коллектор", "Электродрель",
                "Трубогиб", "Перфоратор", "Фрезер", "Газовый паяльник",
                "Клещи для развальцовки", "Набор отверток", "Рулетка",
                "Уровень", "Лазерный уровень", "Мультиметр", "Шуруповерт", "Сварочный аппарат",
                "Тепловизор", "Токовые клещи", "Кусачки", "Плоскогубцы", "Ножовка",
                "Резак для труб", "Набор гаечных ключей", "Набор шестигранных ключей", "Ключ-трещотка",
                "Кабелерез", "Стриппер для снятия изоляции", "Измерительная рулетка", "Молоток",
                "Паяльная станция", "Фен для термоусадки", "Угловая шлифовальная машина", "Тиски",
                "Распределительная коробка", "Кабельные скобы", "Гравировальная машинка", "Осциллограф",
                "Пробник напряжения", "Контактный термометр", "Гидравлический пресс", "Фрезерный станок",
                "Набор напильников", "Металлическая щетка", "Слесарные тиски", "Угломер",
                "Инфракрасный термометр", "Динамометрический ключ", "Пистолет для герметика",
                "Ручной трубогиб", "Электрический лобзик", "Резьбонарезной станок", "Измерительная линейка",
                "Циркуль", "Гидравлический подъемник", "Плоскогубцы с изоляцией", "Набор сверл"
            };

            var random = new Random();
            var toolsStock = warehouseIds.SelectMany(warehouseId =>
                tools.Select(tool => new ToolsStock
                {
                    ID = Guid.NewGuid(),
                    WarehouseId = Guid.Parse(warehouseId),
                    ToolName = tool,
                    Quantity = random.Next(3, 10)
                })).ToList();

            await _toolsCollection.InsertManyAsync(toolsStock);
        }
    }
}