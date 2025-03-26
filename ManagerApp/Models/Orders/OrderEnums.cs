using System.Text.Json.Serialization;

namespace ManagerApp.Models.Orders
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FulfillmentStatus
    {
        New,
        InProgress,
        Completed,
        Cancelled
    }

    /// <summary>
    /// Статус выполнения работы (WorkProgress) в заявке
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WorkProgress
    {
        OrderPlaced,         // Заявка размещена
        OrderProcessed,      // Заявка обработана
        WorkersOnTheRoad,    // Рабочие в пути
        InstallationStarted, // Установка началась
        InstallationCompleted // Установка завершена
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentStatus
    {
        Unpaid,
        Paid
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentMethod
    {
        Mastercard,
        Visa,
        Cash
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderType
    {
        Installation,
        Maintenance
    }
}
