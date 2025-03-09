namespace OrderService.DTO.Technicians
{
    public class UpdateTechnicianStatusDTO
    {
        public string TechnicianId { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string? OrderId { get; set; }
    }
}
