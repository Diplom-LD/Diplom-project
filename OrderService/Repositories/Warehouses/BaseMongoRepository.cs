using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using System.Reflection;

namespace OrderService.Repositories.Warehouses
{
    public abstract class BaseMongoRepository<T> : IStockRepository<T>
    {
        protected readonly IMongoCollection<T> _collection;
        protected readonly ILogger<BaseMongoRepository<T>> _logger;
        private readonly PropertyInfo? _idProperty;
        private readonly PropertyInfo? _nameProperty;

        protected BaseMongoRepository(IMongoCollection<T> collection, ILogger<BaseMongoRepository<T>> logger)
        {
            _collection = collection;
            _logger = logger;

            _idProperty = typeof(T).GetProperties()
                .FirstOrDefault(p => p.Name.Equals("ID", StringComparison.OrdinalIgnoreCase) ||
                                     p.GetCustomAttribute<BsonIdAttribute>() != null);

            _nameProperty = typeof(T).GetProperties()
                .FirstOrDefault(p => p.Name.Equals("Name", StringComparison.OrdinalIgnoreCase));

            if (_idProperty == null)
                throw new InvalidOperationException($"{typeof(T).Name} должен содержать свойство ID.");
        }

        public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _collection.Find(_ => true).ToListAsync(cancellationToken);

        public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateId(id);
            try
            {
                var filter = Builders<T>.Filter.Eq(_idProperty!.Name, id);
                return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при поиске {EntityName} по ID {ID}", typeof(T).Name, id);
                throw;
            }
        }

        public async Task<T?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (_nameProperty == null)
                throw new InvalidOperationException($"{typeof(T).Name} должен содержать свойство Name.");

            var filter = Builders<T>.Filter.Eq(_nameProperty.Name, name);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<T>> GetByWarehouseIdAsync(string warehouseId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(warehouseId))
                throw new ArgumentException("WarehouseId не может быть пустым.", nameof(warehouseId));

            var filter = Builders<T>.Filter.Eq("WarehouseId", warehouseId);
            return await _collection.Find(filter).ToListAsync(cancellationToken);
        }



        public async Task<string> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity), $"{typeof(T).Name} не может быть null.");

            try
            {
                await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
                var id = _idProperty?.GetValue(entity)?.ToString() ?? "Неизвестный ID";
                _logger.LogInformation("Добавлено {EntityName} с ID {ID}", typeof(T).Name, id);
                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении {EntityName}.", typeof(T).Name);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity is null)
                throw new ArgumentNullException(nameof(entity), $"{typeof(T).Name} не может быть null.");

            var id = _idProperty?.GetValue(entity)?.ToString();
            ValidateId(id);

            try
            {
                var filter = Builders<T>.Filter.Eq(_idProperty!.Name, id);
                var result = await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении {EntityName} с ID {ID}", typeof(T).Name, id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateId(id);

            try
            {
                var filter = Builders<T>.Filter.Eq(_idProperty!.Name, id);
                var result = await _collection.DeleteOneAsync(filter, cancellationToken);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении {EntityName} с ID {ID}", typeof(T).Name, id);
                throw;
            }
        }

        private static void ValidateId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID не может быть пустым.", nameof(id));
        }
    }
}
