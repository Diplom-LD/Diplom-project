using System.ComponentModel.DataAnnotations;
using OrderService.Models.Enums;
using System.Text.Json.Serialization;

namespace OrderService.DTO.Orders.CreateOrders
{
    public class CreateOrderRequestForClient : CreateOrderRequestBase
    {
        public override bool IsManager => false;
        public override Guid? ManagerId => null;

        [Required(ErrorMessage = "ClientId обязателен.")]
        public override Guid? ClientId { get; set; } 

        [Required(ErrorMessage = "Дата установки обязательна.")]
        public override DateTime InstallationDate { get; set; }

        [Required(ErrorMessage = "Адрес установки обязателен.")]
        [StringLength(255, MinimumLength = 5, ErrorMessage = "Адрес должен содержать от 5 до 255 символов.")]
        public override required string InstallationAddress { get; set; }

        public override string Notes { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Required(ErrorMessage = "Способ оплаты обязателен.")]
        public override PaymentMethod PaymentMethod { get; set; }

        /// <summary>
        /// ✅ Валидация заявки клиента
        /// </summary>
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            ArgumentNullException.ThrowIfNull(validationContext);

            if (!ClientId.HasValue || ClientId == Guid.Empty)
            {
                yield return new ValidationResult("ClientId не может быть пустым.", [nameof(ClientId)]);
            }
        }
    }
}
