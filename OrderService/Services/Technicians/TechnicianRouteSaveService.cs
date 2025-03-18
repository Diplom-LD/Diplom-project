using OrderService.Data.Orders;
using OrderService.DTO.GeoLocation;

namespace OrderService.Services.Technicians
{
    public class TechnicianRouteSaveService(OrderDbContext dbContext, ILogger<TechnicianRouteSaveService> logger)
    {
        private readonly OrderDbContext _dbContext = dbContext;
        private readonly ILogger<TechnicianRouteSaveService> _logger = logger;

        /// 📌 Сохранение первоначальных маршрутов техников
        public async Task SaveInitialRoutesAsync(Guid orderId, List<RouteDTO> routes)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order == null)
            {
                _logger.LogError("❌ Заявка {OrderId} не найдена. Невозможно сохранить маршрут.", orderId);
                return;
            }

            order.SetInitialRoutes(routes);
            await _dbContext.SaveChangesAsync(); 
            _logger.LogInformation("✅ Первоначальный маршрут для заявки {OrderId} сохранён в БД.", orderId);
        }

        /// 📌 Сохранение финальных маршрутов техников
        public async Task SaveFinalRoutesAsync(Guid orderId, List<RouteDTO> routes)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order == null)
            {
                _logger.LogError("❌ Заявка {OrderId} не найдена. Невозможно сохранить финальный маршрут.", orderId);
                return;
            }

            order.SetFinalRoutes(routes);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("✅ Финальный маршрут для заявки {OrderId} сохранён в БД.", orderId);
        }

        /// 📌 Получить первоначальные маршруты заявки
        public async Task<List<RouteDTO>> GetInitialRoutesAsync(Guid orderId)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            return order?.GetInitialRoutes() ?? [];
        }

        /// 📌 Получить финальные маршруты заявки
        public async Task<List<RouteDTO>> GetFinalRoutesAsync(Guid orderId)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            return order?.GetFinalRoutes() ?? [];
        }
    }
}
