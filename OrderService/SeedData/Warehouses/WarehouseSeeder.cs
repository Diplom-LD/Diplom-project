using MongoDB.Driver;
using OrderService.Data.Warehouses;
using OrderService.Models.Warehouses;
using OrderService.Services.GeoLocation;

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
                    ID = Guid.NewGuid().ToString(),
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
                    ID = Guid.NewGuid().ToString(),
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
                                             .Project(w => w.ID)
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
                "Hitachi RAS-S10"
            };

            var random = new Random();
            var equipmentStock = warehouseIds.SelectMany(warehouseId =>
                models.Select(model => new EquipmentStock
                {
                    ID = Guid.NewGuid().ToString(),
                    WarehouseId = warehouseId,
                    ModelName = model,
                    BTU = random.Next(9000, 30000),
                    ServiceArea = random.Next(20, 100),
                    Price = random.Next(45000, 95000),
                    Quantity = random.Next(5, 15)
                })).ToList();

            await _equipmentCollection.InsertManyAsync(equipmentStock);
        }


        private async Task SeedMaterialsStock(List<string> warehouseIds)
        {
            if (warehouseIds.Count == 0 || await _materialsCollection.CountDocumentsAsync(FilterDefinition<MaterialsStock>.Empty) > 0)
                return;

            var materials = new List<string>
            {
                "Медная трубка 1/4 дюйма", "Фреон R410A", "Крепежные анкера",
                "Теплоизоляция для труб", "Дренажный шланг", "Гофрированная труба",
                "Монтажный профиль", "Фильтр для кондиционера", "Кабель для подключения",
                "Антикоррозийное покрытие"
            };

            var random = new Random();
            var materialsStock = warehouseIds.SelectMany(warehouseId =>
                materials.Select(material => new MaterialsStock
                {
                    ID = Guid.NewGuid().ToString(),
                    WarehouseId = warehouseId,
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
                "Клещи для развальцовки", "Набор отверток", "Рулетка"
            };

            var random = new Random();
            var toolsStock = warehouseIds.SelectMany(warehouseId =>
                tools.Select(tool => new ToolsStock
                {
                    ID = Guid.NewGuid().ToString(),
                    WarehouseId = warehouseId,
                    ToolName = tool,
                    Quantity = random.Next(1, 10)
                })).ToList();

            await _toolsCollection.InsertManyAsync(toolsStock);
        }
    }
}
