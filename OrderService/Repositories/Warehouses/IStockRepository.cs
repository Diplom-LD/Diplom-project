namespace OrderService.Repositories.Warehouses
{
    public interface IStockRepository<T>
    {
        Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<T?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<List<T>> GetByWarehouseIdAsync(string warehouseId, CancellationToken cancellationToken = default);
        Task<string> AddAsync(T entity, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
