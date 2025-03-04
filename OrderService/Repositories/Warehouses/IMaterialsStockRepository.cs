using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderService.Models.Warehouses;

namespace OrderService.Repositories.Warehouses
{
    public interface IMaterialsStockRepository
    {
        Task<List<MaterialsStock>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<MaterialsStock?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<MaterialsStock?> GetByMaterialNameAsync(string materialName, CancellationToken cancellationToken = default);
        Task AddAsync(MaterialsStock material, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(MaterialsStock material, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
