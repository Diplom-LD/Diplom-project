namespace OrderService.DTO.Warehouses
{
    public class WarehouseDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public List<EquipmentStockDTO> Equipment { get; set; } = [];
        public List<MaterialsStockDTO> Materials { get; set; } = [];
        public List<ToolsStockDTO> Tools { get; set; } = [];
    }

    public class EquipmentStockDTO
    {
        public Guid Id { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public int BTU { get; set; }
        public int ServiceArea { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class MaterialsStockDTO
    {
        public Guid Id { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class ToolsStockDTO
    {
        public Guid Id { get; set; }
        public string ToolName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
