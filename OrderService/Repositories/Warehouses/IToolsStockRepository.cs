using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrderService.Models.Warehouses;

namespace OrderService.Repositories.Warehouses
{
    public interface IToolsStockRepository
    {
        Task<List<ToolsStock>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<ToolsStock?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<ToolsStock?> GetByToolNameAsync(string toolName, CancellationToken cancellationToken = default);
        Task AddAsync(ToolsStock tool, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(ToolsStock tool, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
