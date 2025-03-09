using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace OrderService.Models.Warehouses
{
    public partial class Warehouse
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string ID { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("name")]
        [Required(ErrorMessage = "Название склада обязательно.")]
        [StringLength(100, ErrorMessage = "Название склада не может превышать 100 символов.")]
        public string Name { get; set; } = null!;

        [BsonElement("address")]
        [Required(ErrorMessage = "Адрес склада обязателен.")]
        [StringLength(200, ErrorMessage = "Адрес склада не может превышать 200 символов.")]
        public string Address { get; set; } = null!;

        [BsonElement("contactPerson")]
        [Required(ErrorMessage = "Контактное лицо обязательно.")]
        [StringLength(50, ErrorMessage = "Имя контактного лица не может превышать 50 символов.")]
        public string ContactPerson { get; set; } = null!;

        [BsonElement("phoneNumber")]
        [Required(ErrorMessage = "Телефон склада обязателен.")]
        [RegularExpression(@"^\+?[1-9]\d{7,14}$", ErrorMessage = "Некорректный формат номера телефона.")]
        public string PhoneNumber { get; set; } = null!;

        [BsonElement("lastInventoryCheck")]
        public DateTime LastInventoryCheck { get; set; } = DateTime.UtcNow;

        [BsonElement("latitude")]
        public double Latitude { get; set; }

        [BsonElement("longitude")]
        public double Longitude { get; set; }

        public Warehouse() { }

        public Warehouse(string name, string address, string contactPerson, string phoneNumber, double latitude, double longitude)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
                throw new ArgumentException("Название склада не может быть пустым или длиннее 100 символов.", nameof(name));

            if (string.IsNullOrWhiteSpace(address) || address.Length > 200)
                throw new ArgumentException("Адрес склада не может быть пустым или длиннее 200 символов.", nameof(address));

            if (string.IsNullOrWhiteSpace(contactPerson) || contactPerson.Length > 50)
                throw new ArgumentException("Контактное лицо не может быть пустым или длиннее 50 символов.", nameof(contactPerson));

            if (string.IsNullOrWhiteSpace(phoneNumber) || !PhoneRegex().IsMatch(phoneNumber))
                throw new ArgumentException("Некорректный формат номера телефона.", nameof(phoneNumber));

            Name = name;
            Address = address;
            ContactPerson = contactPerson;
            PhoneNumber = phoneNumber;
            Latitude = latitude;
            Longitude = longitude;
            LastInventoryCheck = DateTime.UtcNow;
        }

        [GeneratedRegex(@"^\+?[1-9]\d{7,14}$")]
        private static partial Regex PhoneRegex();
    }
}
