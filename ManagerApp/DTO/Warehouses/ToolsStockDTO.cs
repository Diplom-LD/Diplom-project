using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ManagerApp.DTO.Warehouses
{
    public class ToolsStockDTO
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; }

        [Required(ErrorMessage = "ID склада обязателен.")]
        public string WarehouseId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Название инструмента обязательно.")]
        [StringLength(100, ErrorMessage = "Название инструмента не может превышать 100 символов.")]
        public string ToolName { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Количество не может быть отрицательным.")]
        public int Quantity { get; set; }
    }
}
