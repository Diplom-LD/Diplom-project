using Microsoft.EntityFrameworkCore;
using OrderService.Models.Orders;
using OrderService.Data.Orders;

namespace OrderService.Repositories.Orders
{
    public class OrderRepository(OrderDbContext context, ILogger<OrderRepository> logger)
    {
        private readonly OrderDbContext _context = context;
        private readonly ILogger<OrderRepository> _logger = logger;

        /// <summary>
        /// 📌 Создание новой заявки
        /// </summary>
        public async Task CreateOrderAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("📌 Заявка создана с ID: {OrderId}", order.Id);
        }

        /// <summary>
        /// 🔄 Обновление заявки
        /// </summary>
        public async Task UpdateOrderAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("🔄 Заявка обновлена с ID: {OrderId}", order.Id);
        }

        /// <summary>
        /// 🗑️ Удаление заявки
        /// </summary>
        public async Task<bool> DeleteOrderAsync(Guid orderId) 
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("❌ Заявка с ID: {OrderId} не найдена", orderId);
                return false;
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("🗑️ Заявка удалена с ID: {OrderId}", orderId);
            return true;
        }

        /// <summary>
        /// Получение заявки по ID
        /// </summary>
        public async Task<Order?> GetOrderByIdAsync(Guid orderId) 
        {
            return await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Manager)
                .Include(o => o.Equipment)
                .Include(o => o.AssignedTechnicians)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        /// <summary>
        /// Получение всех заявок
        /// </summary>
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Manager)
                .Include(o => o.Equipment)
                .Include(o => o.AssignedTechnicians)
                .ToListAsync();
        }
    }
}
