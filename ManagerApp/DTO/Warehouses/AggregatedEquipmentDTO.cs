using System.Text.Json.Serialization;

namespace ManagerApp.DTO.Warehouses
{
    public class AggregatedEquipmentDTO
    {
        [JsonPropertyName("modelName")]
        public string ModelName { get; set; } = string.Empty;

        [JsonPropertyName("btu")]
        public int ModelBTU { get; set; }

        [JsonPropertyName("serviceArea")]
        public int ServiceArea { get; set; }

        [JsonPropertyName("price")]
        public decimal ModelPrice { get; set; }

        [JsonPropertyName("totalQuantity")]
        public int TotalQuantity { get; set; }
    }
}
