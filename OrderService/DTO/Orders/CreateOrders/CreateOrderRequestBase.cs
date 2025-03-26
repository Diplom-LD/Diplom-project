using System.ComponentModel.DataAnnotations;
using OrderService.Models.Enums;

namespace OrderService.DTO.Orders.CreateOrders
{
    public abstract class CreateOrderRequestBase : IValidatableObject
    {
        public abstract DateTimeOffset InstallationDate { get; set; }
        public abstract string InstallationAddress { get; set; }
        public abstract string Notes { get; set; }
        public abstract PaymentMethod PaymentMethod { get; set; }
        public abstract bool IsManager { get; }

        public virtual Guid? ManagerId { get; set; } 
        public virtual Guid? ClientId { get; set; } 

        /// <summary>
        /// Валидация заявки
        /// </summary>
        public abstract IEnumerable<ValidationResult> Validate(ValidationContext validationContext);
    }
}
