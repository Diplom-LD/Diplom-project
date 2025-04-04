using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ManagerApp.DTO.Warehouses
{
    public class EquipmentStockDTO
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; }

        [Required(ErrorMessage = "ID склада обязателен.")]
        public string WarehouseId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Название модели обязательно.")]
        [StringLength(100, ErrorMessage = "Название модели не может превышать 100 символов.")]
        public string ModelName { get; set; } = string.Empty;

        [Range(1000, 300000, ErrorMessage = "BTU должен быть в диапазоне от 1000 до 300000.")]
        public int BTU { get; set; }

        [Range(1, 500, ErrorMessage = "Площадь обслуживания должна быть от 1 до 500 м².")]
        public int ServiceArea { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0.")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество не может быть отрицательным.")]
        public int Quantity { get; set; }
    }
}
