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

            if (settings?.Value == null || string.IsNullOrEmpty(settings.Value.ConnectionString) || string.IsNullOrEmpty(settings.Value.DatabaseName))
            {
                const string errorMessage = "❌ Ошибка конфигурации: Отсутствует строка подключения или имя базы данных.";
                _logger.LogError(errorMessage);
                throw new ArgumentException(errorMessage);
            }

            try
            {
                var mongoClientSettings = MongoClientSettings.FromConnectionString(settings.Value.ConnectionString);
                mongoClientSettings.ConnectTimeout = TimeSpan.FromSeconds(10);
                mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);

                var client = new MongoClient(mongoClientSettings);
                _database = client.GetDatabase(settings.Value.DatabaseName);

                _logger.LogInformation("✅ MongoDB подключен: {DatabaseName}", settings.Value.DatabaseName);

                EnsureIndexes();
            }
            catch (MongoConfigurationException ex)
            {
                _logger.LogCritical(ex, "❌ Ошибка конфигурации MongoDB: {Message}", ex.Message);
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogCritical(ex, "⏳ Таймаут подключения к MongoDB: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "❌ Ошибка подключения к MongoDB: {Message}", ex.Message);
                throw;
            }
        }

        public IMongoCollection<Warehouse> Warehouses => GetCollection<Warehouse>("Warehouses");
        public IMongoCollection<EquipmentStock> EquipmentStock => GetCollection<EquipmentStock>("EquipmentStock");
        public IMongoCollection<ToolsStock> ToolsStock => GetCollection<ToolsStock>("ToolsStock");
        public IMongoCollection<MaterialsStock> MaterialsStock => GetCollection<MaterialsStock>("MaterialsStock");

        private IMongoCollection<T> GetCollection<T>(string name)
        {
            try
            {
                var collection = _database.GetCollection<T>(name);
                _logger.LogInformation("📦 Коллекция {CollectionName} успешно получена.", name);
                return collection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка получения коллекции {CollectionName} в MongoDB", name);
                throw;
            }
        }

        private void EnsureIndexes()
        {
            try
            {
                _logger.LogInformation("📌 Создание индексов для MongoDB...");

                CreateUniqueIndex(Warehouses, w => w.Name, "Warehouse_Name_Index");
                CreateIndex(EquipmentStock, e => e.ModelName, "Equipment_Model_Index");
                CreateIndex(ToolsStock, t => t.ToolName, "Tools_Name_Index");
                CreateIndex(MaterialsStock, m => m.MaterialName, "Materials_Name_Index");

                _logger.LogInformation("✅ Индексы MongoDB успешно созданы.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "❌ Ошибка при создании индексов MongoDB: {Message}", ex.Message);
                throw;
            }
        }

        private void CreateIndex<T>(IMongoCollection<T> collection, System.Linq.Expressions.Expression<Func<T, object>> field, string indexName)
        {
            try
            {
                var keys = Builders<T>.IndexKeys.Ascending(field);
                var indexModel = new CreateIndexModel<T>(keys, new CreateIndexOptions { Name = indexName });
                var result = collection.Indexes.CreateOne(indexModel);

                _logger.LogInformation("✅ Индекс {IndexName} создан. Результат: {Result}", indexName, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при создании индекса {IndexName}: {Message}", indexName, ex.Message);
            }
        }

        private void CreateUniqueIndex<T>(IMongoCollection<T> collection, System.Linq.Expressions.Expression<Func<T, object>> field, string indexName)
        {
            try
            {
                var keys = Builders<T>.IndexKeys.Ascending(field);
                var indexModel = new CreateIndexModel<T>(keys, new CreateIndexOptions { Unique = true, Name = indexName });
                var result = collection.Indexes.CreateOne(indexModel);

                _logger.LogInformation("✅ Уникальный индекс {IndexName} создан. Результат: {Result}", indexName, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при создании уникального индекса {IndexName}: {Message}", indexName, ex.Message);
            }
        }
    }
}
