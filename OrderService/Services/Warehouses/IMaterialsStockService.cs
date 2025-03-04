using OrderService.Models.Warehouses;

namespace OrderService.Services.Warehouses
{
    public interface IMaterialsStockService
    {
        Task<List<MaterialsStock>> GetAllAsync();
        Task<MaterialsStock?> GetByIdAsync(string id);
        Task AddAsync(MaterialsStock material);
        Task<bool> UpdateAsync(MaterialsStock material);
        Task<bool> DeleteAsync(string id);
    }
}
