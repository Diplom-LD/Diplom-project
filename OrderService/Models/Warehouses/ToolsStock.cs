using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Models.Warehouses
{
    public class ToolsStock
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid ID { get; set; } = Guid.NewGuid(); 

        [BsonElement("warehouseId")]
        [BsonRepresentation(BsonType.String)]
        [Required(ErrorMessage = "ID склада обязателен.")]
        public Guid WarehouseId { get; set; } 

        [BsonElement("toolName")]
        [Required(ErrorMessage = "Название инструмента обязательно.")]
        [StringLength(100, ErrorMessage = "Название инструмента не может превышать 100 символов.")]
        public string ToolName { get; set; } = string.Empty; 

        [BsonElement("quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Количество инструмента не может быть отрицательным.")]
        public int Quantity { get; set; }

        public ToolsStock() { }

        public ToolsStock(Guid warehouseId, string toolName, int quantity)
        {
            ID = Guid.NewGuid(); 
            WarehouseId = warehouseId;
            ToolName = toolName;
            Quantity = quantity;
        }
    }
}
