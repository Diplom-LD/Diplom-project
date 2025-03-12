namespace OrderService.DTO.Technicians
{
    public class UpdateTechnicianStatusDTO
    {
        public Guid TechnicianId { get; set; }  
        public bool IsAvailable { get; set; }
        public Guid? OrderId { get; set; } 
    }
}
