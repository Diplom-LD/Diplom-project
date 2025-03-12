namespace OrderService.DTO.Orders
{
    public class EquipmentDTO
    {
        public Guid Id { get; set; }
        public required string ModelName { get; set; }
        public int BTU { get; set; }
        public int ServiceArea { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
