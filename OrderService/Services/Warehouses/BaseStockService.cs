using OrderService.Repositories.Warehouses;

namespace OrderService.Services.Warehouses
{
    public abstract class BaseStockService<T>(IStockRepository<T> repository, ILogger<BaseStockService<T>> logger)
    {
        protected readonly IStockRepository<T> _repository = repository;
        protected readonly ILogger<BaseStockService<T>> _logger = logger;

        public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _repository.GetAllAsync(cancellationToken);

        public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateId(id);
            return await _repository.GetByIdAsync(id, cancellationToken);
        }

        public virtual async Task<string> AddAsync(T item, CancellationToken cancellationToken = default)
        {
            ValidateStockItem(item);
            var id = await _repository.AddAsync(item, cancellationToken);
            LogAction("Добавлено", item, id);
            return id;
        }

        public virtual async Task<bool> UpdateAsync(T item, CancellationToken cancellationToken = default)
        {
            ValidateStockItem(item);
            var updated = await _repository.UpdateAsync(item, cancellationToken);
            if (updated)
            {
                var id = item?.GetType().GetProperty("ID")?.GetValue(item)?.ToString() ?? "Неизвестный ID";
                LogAction("Обновлено", item, id);
            }
            return updated;
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateId(id);
            var deleted = await _repository.DeleteAsync(id, cancellationToken);
            if (deleted) LogAction("Удалено", default, id);
            return deleted;
        }

        protected static void ValidateId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID не может быть пустым.", nameof(id));
        }

        protected abstract void ValidateStockItem(T item);

        protected virtual void LogAction(string action, T? item, string id)
        {
            _logger.LogInformation("{Action} {EntityType}: {EntityDetails} (ID: {ID})",
                action, typeof(T).Name, item?.ToString() ?? "Неизвестный элемент", id);
        }
    }
}
