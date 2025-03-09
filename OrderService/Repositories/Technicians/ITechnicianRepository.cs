using OrderService.Models.Technicians;

namespace OrderService.Repositories.Technicians
{
    public interface ITechnicianRepository
    {
        Task<List<Technician>> GetAllAsync();
        Task<Technician?> GetByIdAsync(string id);
        Task SaveAsync(List<Technician> technicians);
        Task UpdateTechnicianAsync(Technician technician);
    }
}
