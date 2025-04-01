namespace ManagerApp.DTO.Orders
{
    public class AllLocationsResponseDTO
    {
        public List<TechnicianLocationDTO> Technicians { get; set; } = [];
        public List<WarehouseLocationDTO> Warehouses { get; set; } = [];
    }

    public class TechnicianLocationDTO
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class WarehouseLocationDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
