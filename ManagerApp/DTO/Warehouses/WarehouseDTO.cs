using System.ComponentModel.DataAnnotations;

namespace ManagerApp.DTO.Warehouses
{
    public class WarehouseDTO
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Название склада обязательно.")]
        [StringLength(100, ErrorMessage = "Название не может превышать 100 символов.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Адрес обязателен.")]
        [StringLength(200, ErrorMessage = "Адрес не может превышать 200 символов.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Контактное лицо обязательно.")]
        [StringLength(50, ErrorMessage = "Контактное лицо не может превышать 50 символов.")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефон обязателен.")]
        [RegularExpression(@"^\+?[1-9]\d{7,14}$", ErrorMessage = "Некорректный формат телефона.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Дата последней инвентаризации обязательна.")]
        public DateTimeOffset LastInventoryCheck { get; set; }

        [Range(-90, 90, ErrorMessage = "Широта должна быть в диапазоне от -90 до 90.")]
        public double Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Долгота должна быть в диапазоне от -180 до 180.")]
        public double Longitude { get; set; }
    }
}
