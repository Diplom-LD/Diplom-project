using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace OrderService.Models.Warehouses
{
    public partial class Warehouse
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid ID { get; set; } = Guid.NewGuid(); 

        [BsonElement("name")]
        [Required(ErrorMessage = "Название склада обязательно.")]
        [StringLength(100, ErrorMessage = "Название склада не может превышать 100 символов.")]
        public string Name { get; set; } = string.Empty; 

        [BsonElement("address")]
        [Required(ErrorMessage = "Адрес склада обязателен.")]
        [StringLength(200, ErrorMessage = "Адрес склада не может превышать 200 символов.")]
        public string Address { get; set; } = string.Empty;

        [BsonElement("contactPerson")]
        [Required(ErrorMessage = "Контактное лицо обязательно.")]
        [StringLength(50, ErrorMessage = "Имя контактного лица не может превышать 50 символов.")]
        public string ContactPerson { get; set; } = string.Empty;

        [BsonElement("phoneNumber")]
        [Required(ErrorMessage = "Телефон склада обязателен.")]
        [RegularExpression(@"^\+?[1-9]\d{7,14}$", ErrorMessage = "Некорректный формат номера телефона.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [BsonElement("lastInventoryCheck")]
        public DateTimeOffset LastInventoryCheck { get; set; } = DateTime.UtcNow;

        [BsonElement("latitude")]
        public double Latitude { get; set; }

        [BsonElement("longitude")]
        public double Longitude { get; set; }

        public Warehouse() { } 

        public Warehouse(string name, string address, string contactPerson, string phoneNumber, double latitude, double longitude)
        {
            ID = Guid.NewGuid();
            Name = ValidateString(name, 100, "Название склада");
            Address = ValidateString(address, 200, "Адрес склада");
            ContactPerson = ValidateString(contactPerson, 50, "Контактное лицо");
            PhoneNumber = ValidatePhoneNumber(phoneNumber);
            Latitude = latitude;
            Longitude = longitude;
            LastInventoryCheck = DateTime.UtcNow;
        }

        private static string ValidateString(string value, int maxLength, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
                throw new ArgumentException($"{fieldName} не может быть пустым или длиннее {maxLength} символов.", nameof(value));
            return value;
        }

        private static string ValidatePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || !PhoneRegex().IsMatch(phoneNumber))
                throw new ArgumentException("Некорректный формат номера телефона.", nameof(phoneNumber));
            return phoneNumber;
        }

        [GeneratedRegex(@"^\+?[1-9]\d{7,14}$")]
        private static partial Regex PhoneRegex();
    }
}
