using OrderService.Models.Warehouses;

namespace OrderService.Services.Warehouses
{
    public interface IWarehouseService
    {
        Task<List<Warehouse>> GetAllAsync();
        Task<Warehouse?> GetByIdAsync(string id);
        Task CreateAsync(Warehouse warehouse);
        Task<bool> UpdateAsync(Warehouse warehouse);
        Task<bool> DeleteAsync(string id);
    }
}
