namespace ManagerApp.Clients
{
    using ManagerApp.DTO.Orders;
    using ManagerApp.DTO.Technicians;
    using ManagerApp.Models.Orders;

    public interface IOrderServiceClient
    {
        Task<List<OrderResponse>> GetAllOrdersAsync(string accessToken);
        Task<CreatedOrderResponseDTO?> CreateOrderAsync(OrderRequest request, string accessToken);
        Task<bool> UpdateOrderStatusAsync(Guid orderId, FulfillmentStatus newStatus, string accessToken);
        Task<OrderResponse?> GetOrderByIdAsync(Guid orderId, string accessToken);
        Task<bool> UpdateOrderFieldsAsync(OrderUpdateRequestDTO dto, string accessToken);
        Task<List<TechnicianDTO>> GetAvailableTechniciansTodayAsync(string accessToken);
        Task<bool> CheckTechniciansArrivalAsync(Guid orderId, string accessToken);
    }
}
