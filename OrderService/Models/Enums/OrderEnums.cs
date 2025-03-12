namespace OrderService.Models.Enums
{
    public enum FulfillmentStatus
    {
        New,
        InProgress,
        Completed,
        Cancelled
    }

    public enum WorkProgress
    {
        OrderPlaced,
        OrderProcessed,
        WorkersOnTheRoad,
        InstallationStarted,
        InstallationCompleted
    }

    public enum PaymentStatus
    {
        UnPaid,
        Paid
    }

    public enum PaymentMethod
    {
        Mastercard,
        Visa,
        Cash
    }

    public enum OrderType
    {
        Installation, 
        Maintenance   
    }
}
