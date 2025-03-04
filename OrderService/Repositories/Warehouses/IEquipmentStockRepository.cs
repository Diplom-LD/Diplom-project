using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderService.Models.Warehouses;

namespace OrderService.Repositories.Warehouses
{
    public interface IEquipmentStockRepository
    {
        Task<List<EquipmentStock>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<EquipmentStock?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<EquipmentStock?> GetByModelNameAsync(string modelName, CancellationToken cancellationToken = default);
        Task AddAsync(EquipmentStock equipment, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(EquipmentStock equipment, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
