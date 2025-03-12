using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Models.Warehouses
{
    public class MaterialsStock
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid ID { get; set; } = Guid.NewGuid(); 

        [BsonElement("warehouseId")]
        [BsonRepresentation(BsonType.String)]
        [Required(ErrorMessage = "ID склада обязателен.")]
        public Guid WarehouseId { get; set; } 

        [BsonElement("materialName")]
        [Required(ErrorMessage = "Название материала обязательно.")]
        [StringLength(100, ErrorMessage = "Название материала не может превышать 100 символов.")]
        public string MaterialName { get; set; } = string.Empty;

        [BsonElement("quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Количество материала не может быть отрицательным.")]
        public int Quantity { get; set; }

        [BsonElement("price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена материала должна быть больше 0.")]
        public decimal Price { get; set; }

        public MaterialsStock() { }

        public MaterialsStock(Guid warehouseId, string materialName, int quantity, decimal price)
        {
            ID = Guid.NewGuid(); 
            WarehouseId = warehouseId;
            MaterialName = materialName;
            Quantity = quantity;
            Price = price;
        }
    }
}
