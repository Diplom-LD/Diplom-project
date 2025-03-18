using Microsoft.EntityFrameworkCore;
using OrderService.Models.Orders;
using OrderService.Data.Orders;
using OrderService.Models.Enums;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using OrderService.DTO.Users;

namespace OrderService.Repositories.Orders
{
    public class OrderRepository(OrderDbContext context, ILogger<OrderRepository> logger)
    {
        private readonly OrderDbContext _context = context;
        private readonly ILogger<OrderRepository> _logger = logger;

        /// <summary>
        /// 📌 Создание новой заявки (с проверкой на существование)
        /// </summary>
        public async Task<bool> CreateOrderAsync(Order order)
        {
            if (await _context.Orders.AnyAsync(o => o.Id == order.Id))
            {
                _logger.LogError("❌ Ошибка: Заявка {OrderId} уже существует!", order.Id);
                return false;
            }

            _context.Orders.Add(order); 
            return true;
        }

        public async Task<int> SaveChangesAsync()
        {
            try
            {
                int changes = await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Успешно сохранено {Changes} изменений в базе данных.", changes);
                return changes;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning("⚠️ Конфликт обновления в БД! Повторная попытка...");

                foreach (var entry in ex.Entries)
                {
                    if (entry.Entity is Order order)
                    {
                        _logger.LogWarning("🔄 Повторная загрузка данных для Order ID {OrderId}", order.Id);

                        var freshOrder = await _context.Orders
                            .Include(o => o.Equipment)
                            .Include(o => o.AssignedTechnicians)
                            .Include(o => o.RequiredMaterials)
                            .Include(o => o.RequiredTools)
                            .FirstOrDefaultAsync(o => o.Id == order.Id);

                        if (freshOrder == null)
                        {
                            _logger.LogError("❌ Ошибка: Заявка {OrderId} была удалена во время обновления!", order.Id);
                            throw;
                        }

                        _context.Entry(order).CurrentValues.SetValues(freshOrder);
                    }
                }

                try
                {
                    int retryChanges = await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Успешно сохранено {Changes} изменений после повторной попытки.", retryChanges);
                    return retryChanges;
                }
                catch (DbUpdateConcurrencyException retryEx)
                {
                    _logger.LogError(retryEx, "❌ Ошибка: Повторная попытка сохранения не удалась!");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при сохранении изменений в БД!");
                throw;
            }
        }



        /// <summary>
        /// 🗑️ Удаление заявки (с проверкой существования)
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
        /// 🔍 Получение заявки по ID (с деталями)
        /// </summary>
        public async Task<Order?> GetOrderByIdAsync(Guid orderId, bool includeDetails = false)
        {
            IQueryable<Order> query = _context.Orders;

            if (includeDetails)
            {
                query = query.Include(o => o.Equipment)
                             .Include(o => o.RequiredMaterials)
                             .Include(o => o.RequiredTools)
                             .Include(o => o.AssignedTechnicians);
            }

            return await query.FirstOrDefaultAsync(o => o.Id == orderId);
        }


        /// <summary>
        /// 📋 Получение всех заявок
        /// </summary>
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .AsTracking()
                .Include(o => o.Client)
                .Include(o => o.Manager)
                .Include(o => o.Equipment)
                .Include(o => o.RequiredMaterials)
                .Include(o => o.RequiredTools)
                .Include(o => o.AssignedTechnicians)
                .AsSplitQuery()
                .ToListAsync();
        }

        /// <summary>
        /// 🔄 Начало транзакции
        /// </summary>
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public void AttachEntity(Order order)
        {
            _context.Attach(order);
        }


        public async Task<Order?> GetOrderByTechnicianIdAsync(Guid technicianId)
        {
            return await _context.Orders
                .Include(o => o.AssignedTechnicians)
                .FirstOrDefaultAsync(o => o.AssignedTechnicians.Any(t => t.TechnicianID == technicianId)
                                          && o.FulfillmentStatus == FulfillmentStatus.InProgress);
        }

        public async Task<List<TechnicianDTO>> GetTechniciansByOrderIdAsync(Guid orderId)
        {
            _logger.LogInformation("🔍 Получение списка техников для заявки {OrderId}...", orderId);

            var order = await _context.Orders
                .Include(o => o.AssignedTechnicians)
                .ThenInclude(at => at.Technician)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("⚠️ Заявка {OrderId} не найдена!", orderId);
                return [];
            }

            var technicians = order.AssignedTechnicians.Select(at => new TechnicianDTO
            {
                Id = at.Technician.Id,
                FullName = at.Technician.FullName,
                Address = at.Technician.Address,
                PhoneNumber = at.Technician.PhoneNumber,
                Latitude = at.Technician.Latitude,
                Longitude = at.Technician.Longitude,
                IsAvailable = at.Technician.IsAvailable,
                CurrentOrderId = at.Technician.CurrentOrderId
            }).ToList();

            _logger.LogInformation("✅ Найдено {TechnicianCount} техников для заявки {OrderId}", technicians.Count, orderId.ToString());
            return technicians;
        }


        /// <summary>
        /// 🔄 Обновляет существующую заявку в базе данных.
        /// </summary>
        public async Task<bool> UpdateOrderAsync(Order order)
        {
            _logger.LogInformation("🔄 Обновление заявки {OrderId}...", order.Id);

            try
            {
                _context.Orders.Update(order);  
                await _context.SaveChangesAsync(); 

                _logger.LogInformation("✅ Заявка {OrderId} успешно обновлена.", order.Id);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "⚠️ Конфликт обновления заявки {OrderId}. Повторная попытка...", order.Id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при обновлении заявки {OrderId}!", order.Id);
                return false;
            }
        }


    }
}
