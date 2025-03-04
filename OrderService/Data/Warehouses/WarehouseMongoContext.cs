using MongoDB.Driver;
using Microsoft.Extensions.Options;
using OrderService.Config;
using OrderService.Models.Warehouses;
using Microsoft.Extensions.Logging;
using System;

namespace OrderService.Data.Warehouses
{
    public class WarehouseMongoContext
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<WarehouseMongoContext> _logger;

        public WarehouseMongoContext(IOptions<MongoSettings> settings, ILogger<WarehouseMongoContext> logger)
        {
            _logger = logger;

            try
            {
                var mongoClientSettings = MongoClientSettings.FromConnectionString(settings.Value.ConnectionString);
                mongoClientSettings.ConnectTimeout = TimeSpan.FromSeconds(10); 
                mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(10); 

                var client = new MongoClient(mongoClientSettings);
                _database = client.GetDatabase(settings.Value.DatabaseName);

                _logger.LogInformation("MongoDB подключен: {DatabaseName}", settings.Value.DatabaseName);

                EnsureIndexes();
            }
            catch (MongoConfigurationException ex)
            {
                _logger.LogError("Ошибка конфигурации MongoDB: {Message}", ex.Message);
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError("Таймаут подключения к MongoDB: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка подключения к MongoDB: {Message}", ex.Message);
                throw;
            }
        }

        public IMongoCollection<Warehouse> Warehouses =>
            _database.GetCollection<Warehouse>("Warehouses");

        public IMongoCollection<EquipmentStock> EquipmentStock =>
            _database.GetCollection<EquipmentStock>("EquipmentStock");

        public IMongoCollection<ToolsStock> ToolsStock =>
            _database.GetCollection<ToolsStock>("ToolsStock");

        public IMongoCollection<MaterialsStock> MaterialsStock =>
            _database.GetCollection<MaterialsStock>("MaterialsStock");

        private void EnsureIndexes()
        {
            try
            {
                _logger.LogInformation("Создание индексов для MongoDB...");

                Warehouses.Indexes.CreateOne(
                    new CreateIndexModel<Warehouse>(
                        Builders<Warehouse>.IndexKeys.Ascending(w => w.Name),
                        new CreateIndexOptions { Unique = true } 
                    ));

                EquipmentStock.Indexes.CreateOne(
                    new CreateIndexModel<EquipmentStock>(
                        Builders<EquipmentStock>.IndexKeys.Ascending(e => e.ModelName)
                    ));

                ToolsStock.Indexes.CreateOne(
                    new CreateIndexModel<ToolsStock>(
                        Builders<ToolsStock>.IndexKeys.Ascending(t => t.ToolName)
                    ));

                MaterialsStock.Indexes.CreateOne(
                    new CreateIndexModel<MaterialsStock>(
                        Builders<MaterialsStock>.IndexKeys.Ascending(m => m.MaterialName)
                    ));

                _logger.LogInformation("Индексы MongoDB успешно созданы.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при создании индексов MongoDB: {Message}", ex.Message);
                throw;
            }
        }
    }
}
