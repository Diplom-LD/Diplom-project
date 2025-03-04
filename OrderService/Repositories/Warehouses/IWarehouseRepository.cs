using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderService.Models.Warehouses;

namespace OrderService.Repositories.Warehouses
{
    public interface IWarehouseRepository
    {
        Task<List<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Warehouse?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<Warehouse?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task CreateAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
