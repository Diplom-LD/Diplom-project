using OrderService.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OrderService.DTO.Orders.CreateOrders
{
    public class CreateOrderRequestManager : CreateOrderRequestBase
    {
        public override bool IsManager => true;

        [Required(ErrorMessage = "ManagerId обязателен.")]
        public override Guid? ManagerId { get; set; } 

        public override Guid? ClientId => null; 

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Required(ErrorMessage = "Тип заявки обязателен.")]
        public OrderType OrderType { get; set; }

        [Required(ErrorMessage = "Дата установки обязательна.")]
        public override DateTime InstallationDate { get; set; }

        [Required(ErrorMessage = "Адрес установки обязателен.")]
        [StringLength(255, MinimumLength = 5, ErrorMessage = "Адрес должен содержать от 5 до 255 символов.")]
        public override required string InstallationAddress { get; set; }

        public override string Notes { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Required(ErrorMessage = "Способ оплаты обязателен.")]
        public override PaymentMethod PaymentMethod { get; set; }

        [Required(ErrorMessage = "Информация об оборудовании обязательна.")]
        public required EquipmentDTO Equipment { get; set; }

        public List<Guid>? TechnicianIds { get; set; }

        [Required(ErrorMessage = "Имя клиента обязательно.")]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "Номер телефона обязателен.")]
        [Phone(ErrorMessage = "Некорректный номер телефона.")]
        public required string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email обязателен.")]
        [EmailAddress(ErrorMessage = "Некорректный email.")]
        public required string Email { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Required(ErrorMessage = "Статус выполнения обязателен.")]
        public FulfillmentStatus FulfillmentStatus { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Required(ErrorMessage = "Статус оплаты обязателен.")]
        public PaymentStatus PaymentStatus { get; set; }

        [Required(ErrorMessage = "Стоимость работ обязательна.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Стоимость работ должна быть положительной.")]
        public decimal WorkCost { get; set; }

        /// <summary>
        /// ✅ Валидация заявки менеджера
        /// </summary>
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            ArgumentNullException.ThrowIfNull(validationContext);

            if (!ManagerId.HasValue || ManagerId == Guid.Empty)
            {
                yield return new ValidationResult("ManagerId не может быть пустым.", [nameof(ManagerId)]);
            }

            if (TechnicianIds is { Count: > 0 } && TechnicianIds.Any(id => id == Guid.Empty))
            {
                yield return new ValidationResult("ID техника не может быть пустым.", [nameof(TechnicianIds)]);
            }

            foreach (var error in Equipment.Validate(validationContext))
            {
                yield return error;
            }
        }

        public class EquipmentDTO : IValidatableObject
        {
            [Required(ErrorMessage = "Название модели обязательно.")]
            public required string ModelName { get; set; }

            [Required(ErrorMessage = "Источник модели обязателен.")]
            public required string ModelSource { get; set; }

            [Required(ErrorMessage = "BTU обязателен.")]
            [Range(1000, 300000, ErrorMessage = "BTU должен быть в диапазоне 1000-300000.")]
            public required int BTU { get; set; }

            [Required(ErrorMessage = "Площадь обслуживания обязательна.")]
            [Range(5, 200, ErrorMessage = "Площадь обслуживания должна быть в диапазоне 5-200 м².")]
            public required int ServiceArea { get; set; }

            [Required(ErrorMessage = "Цена оборудования обязательна.")]
            [Range(0.01, double.MaxValue, ErrorMessage = "Цена оборудования должна быть положительной.")]
            public required decimal Price { get; set; }

            [Required(ErrorMessage = "Количество оборудования обязательно.")]
            [Range(1, int.MaxValue, ErrorMessage = "Количество должно быть не менее 1.")]
            public required int Quantity { get; set; } = 1;

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (string.IsNullOrWhiteSpace(ModelSource))
                {
                    yield return new ValidationResult("Источник модели обязателен.", [nameof(ModelSource)]);
                }

                if (string.IsNullOrWhiteSpace(ModelName))
                {
                    yield return new ValidationResult("Название модели обязательно.", [nameof(ModelName)]);
                }

                if (BTU is < 1000 or > 300000)
                {
                    yield return new ValidationResult("BTU должен быть в диапазоне 1000-300000.", [nameof(BTU)]);
                }

                if (ServiceArea is < 5 or > 200)
                {
                    yield return new ValidationResult("Площадь обслуживания должна быть в диапазоне 5-200 м².", [nameof(ServiceArea)]);
                }

                if (Price <= 0)
                {
                    yield return new ValidationResult("Цена оборудования должна быть положительной.", [nameof(Price)]);
                }

                if (Quantity <= 0)
                {
                    yield return new ValidationResult("Количество должно быть не менее 1.", [nameof(Quantity)]);
                }
            }
        }
    }
}
