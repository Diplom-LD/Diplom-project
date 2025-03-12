using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Models.Warehouses
{
    public class EquipmentStock
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid ID { get; set; } = Guid.NewGuid(); 

        [BsonElement("warehouseId")]
        [BsonRepresentation(BsonType.String)]
        [Required(ErrorMessage = "ID склада обязателен.")]
        public Guid WarehouseId { get; set; } 

        [BsonElement("modelName")]
        [Required(ErrorMessage = "Название модели обязательно.")]
        [StringLength(100, ErrorMessage = "Название модели не может превышать 100 символов.")]
        public string ModelName { get; set; } = string.Empty;

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

        public EquipmentStock(Guid warehouseId, string modelName, int btu, int serviceArea, decimal price, int quantity)
        {
            ID = Guid.NewGuid(); 
            WarehouseId = warehouseId;
            ModelName = modelName;
            BTU = btu;
            ServiceArea = serviceArea;
            Price = price;
            Quantity = quantity;
        }
    }
}
