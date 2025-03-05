using MongoDB.Driver;
using OrderService.Data.Warehouses;
using OrderService.Models.Warehouses;

namespace OrderService.SeedData.Warehouses
{
    public class WarehouseSeeder(WarehouseMongoContext context)
    {
        private readonly IMongoCollection<Warehouse> _warehouseCollection = context.Warehouses;
        private readonly IMongoCollection<EquipmentStock> _equipmentCollection = context.EquipmentStock;
        private readonly IMongoCollection<MaterialsStock> _materialsCollection = context.MaterialsStock;
        private readonly IMongoCollection<ToolsStock> _toolsCollection = context.ToolsStock;

        public async Task SeedAsync()
        {
            Console.WriteLine("🔹 Seeding started...");
            await SeedWarehouses();
            var warehouseIds = await GetWarehouseIds();
            await SeedEquipmentStock(warehouseIds);
            await SeedMaterialsStock(warehouseIds);
            await SeedToolsStock(warehouseIds);
            Console.WriteLine("✅ Seeding completed!");
        }

        private async Task SeedWarehouses()
        {
            if (await _warehouseCollection.CountDocumentsAsync(FilterDefinition<Warehouse>.Empty) == 0)
            {
                var warehouses = new List<Warehouse>();
                for (int i = 1; i <= 10; i++)
                {
                    warehouses.Add(new()
                    {
                        ID = Guid.NewGuid().ToString(),
                        Name = $"Склад {i}",
                        Address = $"Город {i}, ул. Центральная, {i * 10}",
                        ContactPerson = $"Контакт {i}",
                        PhoneNumber = $"+7903123456{i}",
                        LastInventoryCheck = DateTime.UtcNow
                    });
                }
                await _warehouseCollection.InsertManyAsync(warehouses);
            }
        }

        private async Task<List<string>> GetWarehouseIds()
        {
            return await _warehouseCollection
                .Find(FilterDefinition<Warehouse>.Empty)
                .Project(w => w.ID)
                .ToListAsync();
        }

        private async Task SeedEquipmentStock(List<string> warehouseIds)
        {
            if (warehouseIds.Count == 0) return;

            if (await _equipmentCollection.CountDocumentsAsync(FilterDefinition<EquipmentStock>.Empty) == 0)
            {
                var equipmentStock = new List<EquipmentStock>
                {
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[0], ModelName = "LG Standard Plus", BTU = 9000, ServiceArea = 25, Price = 45000, Quantity = 10 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[1], ModelName = "Samsung AR9500", BTU = 12000, ServiceArea = 35, Price = 55000, Quantity = 9 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[2], ModelName = "Daikin FTXB25C", BTU = 18000, ServiceArea = 50, Price = 75000, Quantity = 8 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[3], ModelName = "Mitsubishi Electric MSZ-HJ25VA", BTU = 24000, ServiceArea = 70, Price = 95000, Quantity = 7 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[4], ModelName = "Haier HSU-12H", BTU = 12000, ServiceArea = 30, Price = 49000, Quantity = 10 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[5], ModelName = "Gree GWH12KF", BTU = 18000, ServiceArea = 50, Price = 67000, Quantity = 6 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[6], ModelName = "Electrolux EACS-12H", BTU = 12000, ServiceArea = 32, Price = 51000, Quantity = 5 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[7], ModelName = "Toshiba RAS-10N3KVR", BTU = 9000, ServiceArea = 22, Price = 43000, Quantity = 4 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[8], ModelName = "Panasonic CS-E12", BTU = 12000, ServiceArea = 30, Price = 52000, Quantity = 3 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[9], ModelName = "Hitachi RAS-S10", BTU = 9000, ServiceArea = 25, Price = 48000, Quantity = 2 }
                };
                await _equipmentCollection.InsertManyAsync(equipmentStock);
            }
        }

        private async Task SeedMaterialsStock(List<string> warehouseIds)
        {
            if (warehouseIds.Count == 0) return;

            if (await _materialsCollection.CountDocumentsAsync(FilterDefinition<MaterialsStock>.Empty) == 0)
            {
                var materialsStock = new List<MaterialsStock>
                {
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[0], MaterialName = "Медная трубка 1/4 дюйма", Quantity = 50, Price = 500 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[1], MaterialName = "Фреон R410A", Quantity = 20, Price = 3500 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[2], MaterialName = "Крепежные анкера", Quantity = 100, Price = 50 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[3], MaterialName = "Теплоизоляция для труб", Quantity = 30, Price = 800 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[4], MaterialName = "Дренажный шланг", Quantity = 40, Price = 600 }
                };
                await _materialsCollection.InsertManyAsync(materialsStock);
            }
        }

        private async Task SeedToolsStock(List<string> warehouseIds)
        {
            if (warehouseIds.Count == 0) return;

            if (await _toolsCollection.CountDocumentsAsync(FilterDefinition<ToolsStock>.Empty) == 0)
            {
                var toolsStock = new List<ToolsStock>
                {
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[0], ToolName = "Вакуумный насос", Quantity = 3 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[1], ToolName = "Манометрический коллектор", Quantity = 5 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[2], ToolName = "Электродрель", Quantity = 4 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[3], ToolName = "Трубогиб", Quantity = 6 },
                    new() { ID = Guid.NewGuid().ToString(), WarehouseId = warehouseIds[4], ToolName = "Перфоратор", Quantity = 3 }
                };
                await _toolsCollection.InsertManyAsync(toolsStock);
            }
        }
    }
}
