namespace OrderService.DTO.Technicians
{
    public class AssignTechniciansRequest
    {
        public List<string>? TechnicianIds { get; set; }
        public string OrderAddress { get; set; } = string.Empty; 
    }
}
