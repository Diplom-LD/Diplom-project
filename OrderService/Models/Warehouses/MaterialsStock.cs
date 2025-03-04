using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Models.Warehouses
{
    public class MaterialsStock
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("warehouseId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [Required(ErrorMessage = "ID склада обязателен.")]
        public string WarehouseId { get; set; } = null!;

        [BsonElement("materialName")]
        [Required(ErrorMessage = "Название материала обязательно.")]
        [StringLength(100, ErrorMessage = "Название материала не может превышать 100 символов.")]
        public string MaterialName { get; set; } = null!;

        [BsonElement("quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Количество материала не может быть отрицательным.")]
        public int Quantity { get; set; }

        [BsonElement("price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена материала должна быть больше 0.")]
        public decimal Price { get; set; }

        public MaterialsStock() { }

        public MaterialsStock(string warehouseId, string materialName, int quantity, decimal price)
        {
            if (string.IsNullOrWhiteSpace(warehouseId) || !ObjectId.TryParse(warehouseId, out _))
                throw new ArgumentException("Некорректный ID склада.", nameof(warehouseId));

            if (string.IsNullOrWhiteSpace(materialName) || materialName.Length > 100)
                throw new ArgumentException("Название материала не может быть пустым или длиннее 100 символов.", nameof(materialName));

            if (quantity < 0)
                throw new ArgumentException("Количество материала не может быть отрицательным.", nameof(quantity));

            if (price <= 0)
                throw new ArgumentException("Цена материала должна быть больше 0.", nameof(price));

            WarehouseId = warehouseId;
            MaterialName = materialName;
            Quantity = quantity;
            Price = price;
        }
    }
}
