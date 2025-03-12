namespace OrderService.DTO.Technicians
{
    public class AssignTechniciansRequest
    {
        public List<Guid>? TechnicianIds { get; set; } 
        public string OrderAddress { get; set; } = string.Empty;
    }
}
