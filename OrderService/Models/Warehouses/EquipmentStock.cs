using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Models.Warehouses
{
    public class EquipmentStock
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("warehouseId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [Required(ErrorMessage = "ID склада обязателен.")]
        public string WarehouseId { get; set; } = null!;

        [BsonElement("modelName")]
        [Required(ErrorMessage = "Название модели обязательно.")]
        [StringLength(100, ErrorMessage = "Название модели не может превышать 100 символов.")]
        public string ModelName { get; set; } = null!;

        [BsonElement("btu")]
        [Range(1000, 300000, ErrorMessage = "BTU должен быть в диапазоне от 1000 до 300000.")]
        public int BTU { get; set; }

        [BsonElement("serviceArea")]
        [Range(1, 500, ErrorMessage = "Площадь обслуживания должна быть от 1 до 500 м².")]
        public int ServiceArea { get; set; }

        [BsonElement("price")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена оборудования должна быть больше 0.")]
        public decimal Price { get; set; }

        [BsonElement("quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Количество оборудования не может быть отрицательным.")]
        public int Quantity { get; set; }

        public EquipmentStock() { }

        public EquipmentStock(string warehouseId, string modelName, int btu, int serviceArea, decimal price, int quantity)
        {
            if (string.IsNullOrWhiteSpace(warehouseId) || !ObjectId.TryParse(warehouseId, out _))
                throw new ArgumentException("Некорректный ID склада.", nameof(warehouseId));

            if (string.IsNullOrWhiteSpace(modelName) || modelName.Length > 100)
                throw new ArgumentException("Название модели не может быть пустым или длиннее 100 символов.", nameof(modelName));

            if (btu < 1000 || btu > 300000)
                throw new ArgumentException("BTU должен быть в диапазоне от 1000 до 300000.", nameof(btu));

            if (serviceArea < 1 || serviceArea > 500)
                throw new ArgumentException("Площадь обслуживания должна быть от 1 до 500 м².", nameof(serviceArea));

            if (price <= 0)
                throw new ArgumentException("Цена оборудования должна быть больше 0.", nameof(price));

            if (quantity < 0)
                throw new ArgumentException("Количество оборудования не может быть отрицательным.", nameof(quantity));

            WarehouseId = warehouseId;
            ModelName = modelName;
            BTU = btu;
            ServiceArea = serviceArea;
            Price = price;
            Quantity = quantity;
        }
    }
}
