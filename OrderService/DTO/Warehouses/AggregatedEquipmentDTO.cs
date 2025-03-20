namespace OrderService.DTO.Warehouses
{
    public class AggregatedEquipmentDTO
    {
        public string ModelName { get; set; } = string.Empty;
        public int BTU { get; set; }  
        public int ServiceArea { get; set; }
        public decimal Price { get; set; }
        public int TotalQuantity { get; set; }
    }


}
