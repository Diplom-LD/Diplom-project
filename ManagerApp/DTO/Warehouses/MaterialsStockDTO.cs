using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ManagerApp.DTO.Warehouses
{
    public class MaterialsStockDTO
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; }

        [Required(ErrorMessage = "ID склада обязателен.")]
        public string WarehouseId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Название материала обязательно.")]
        [StringLength(100, ErrorMessage = "Название материала не может превышать 100 символов.")]
        public string MaterialName { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Количество не может быть отрицательным.")]
        public int Quantity { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0.")]
        public decimal Price { get; set; }
    }
}