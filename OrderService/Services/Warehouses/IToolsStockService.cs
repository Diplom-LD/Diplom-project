using OrderService.Models.Warehouses;

namespace OrderService.Services.Warehouses
{
    public interface IToolsStockService
    {
        Task<List<ToolsStock>> GetAllAsync();
        Task<ToolsStock?> GetByIdAsync(string id);
        Task AddAsync(ToolsStock tool);
        Task<bool> UpdateAsync(ToolsStock tool);
        Task<bool> DeleteAsync(string id);
    }
}
