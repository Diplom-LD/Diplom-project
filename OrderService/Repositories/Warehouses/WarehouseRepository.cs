using MongoDB.Driver;
using OrderService.Data.Warehouses;
using OrderService.Models.Warehouses;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderService.Repositories.Warehouses
{
    public class WarehouseRepository(WarehouseMongoContext context, ILogger<WarehouseRepository> logger)
        : IWarehouseRepository
    {
        private readonly IMongoCollection<Warehouse> _warehouses = context.Warehouses;

        public async Task<List<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _warehouses.Find(_ => true).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении списка складов.");
                return [];
            }
        }

        public async Task<Warehouse?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Передан некорректный ID склада.", nameof(id));

            try
            {
                return await _warehouses.Find(w => w.ID == id)
                                        .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при поиске склада по ID {ID}", id);
                throw;
            }
        }

        public async Task<Warehouse?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название склада не может быть пустым.", nameof(name));

            try
            {
                return await _warehouses.Find(w => w.Name == name)
                                        .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при поиске склада по названию '{Name}'", name);
                throw;
            }
        }

        public async Task CreateAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
        {
            if (warehouse is null)
                throw new ArgumentNullException(nameof(warehouse), "Передан null-объект склада.");

            try
            {
                await _warehouses.InsertOneAsync(warehouse, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при добавлении склада.");
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
        {
            if (warehouse is null)
                throw new ArgumentNullException(nameof(warehouse), "Передан null-объект склада.");

            if (string.IsNullOrWhiteSpace(warehouse.ID))
                throw new ArgumentException("Передан некорректный ID склада.", nameof(warehouse));

            try
            {
                var result = await _warehouses.ReplaceOneAsync(
                    w => w.ID == warehouse.ID, warehouse, cancellationToken: cancellationToken);

                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обновлении склада с ID {ID}", warehouse.ID);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Передан некорректный ID склада.", nameof(id));

            try
            {
                var result = await _warehouses.DeleteOneAsync(w => w.ID == id, cancellationToken);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при удалении склада с ID {ID}", id);
                throw;
            }
        }
    }
}
