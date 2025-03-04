using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Models.Warehouses
{
    public class ToolsStock
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("warehouseId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [Required(ErrorMessage = "ID склада обязателен.")]
        public string WarehouseId { get; set; } = null!;

        [BsonElement("toolName")]
        [Required(ErrorMessage = "Название инструмента обязательно.")]
        [StringLength(100, ErrorMessage = "Название инструмента не может превышать 100 символов.")]
        public string ToolName { get; set; } = null!;

        [BsonElement("quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Количество инструмента не может быть отрицательным.")]
        public int Quantity { get; set; }

        public ToolsStock() { }

        public ToolsStock(string warehouseId, string toolName, int quantity)
        {
            if (string.IsNullOrWhiteSpace(warehouseId) || !ObjectId.TryParse(warehouseId, out _))
                throw new ArgumentException("Некорректный ID склада.", nameof(warehouseId));

            if (string.IsNullOrWhiteSpace(toolName) || toolName.Length > 100)
                throw new ArgumentException("Название инструмента не может быть пустым или длиннее 100 символов.", nameof(toolName));

            if (quantity < 0)
                throw new ArgumentException("Количество инструмента не может быть отрицательным.", nameof(quantity));

            WarehouseId = warehouseId;
            ToolName = toolName;
            Quantity = quantity;
        }
    }
}
