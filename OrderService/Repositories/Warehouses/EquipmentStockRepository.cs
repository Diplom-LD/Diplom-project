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
    public class EquipmentStockRepository(WarehouseMongoContext context, ILogger<EquipmentStockRepository> logger)
        : IEquipmentStockRepository
    {
        private readonly IMongoCollection<EquipmentStock> _equipmentStock = context.EquipmentStock;

        public async Task<List<EquipmentStock>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _equipmentStock.Find(_ => true).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении списка оборудования.");
                return [];
            }
        }

        public async Task<EquipmentStock?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID оборудования не может быть пустым.", nameof(id));

            try
            {
                return await _equipmentStock.Find(e => e.ID == id)
                                            .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при поиске оборудования по ID {ID}", id);
                throw;
            }
        }

        public async Task<EquipmentStock?> GetByModelNameAsync(string modelName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                throw new ArgumentException("Название модели не может быть пустым.", nameof(modelName));

            try
            {
                return await _equipmentStock.Find(e => e.ModelName == modelName)
                                            .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при поиске оборудования по названию '{ModelName}'", modelName);
                throw;
            }
        }

        public async Task AddAsync(EquipmentStock equipment, CancellationToken cancellationToken = default)
        {
            if (equipment == null)
                throw new ArgumentNullException(nameof(equipment), "Объект оборудования не может быть null.");

            try
            {
                await _equipmentStock.InsertOneAsync(equipment, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при добавлении оборудования.");
                throw;
            }
        }

        public async Task<bool> UpdateAsync(EquipmentStock equipment, CancellationToken cancellationToken = default)
        {
            if (equipment is null)
                throw new ArgumentNullException(nameof(equipment), "Передан null-объект оборудования.");

            if (string.IsNullOrWhiteSpace(equipment.ID))
                throw new ArgumentException("Передан некорректный ID оборудования: пустой или содержит только пробелы.", nameof(equipment));

            try
            {
                var result = await _equipmentStock.ReplaceOneAsync(
                    e => e.ID == equipment.ID, equipment, cancellationToken: cancellationToken);

                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обновлении оборудования с ID {ID}", equipment.ID);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID оборудования не может быть пустым.", nameof(id));

            try
            {
                var result = await _equipmentStock.DeleteOneAsync(e => e.ID == id, cancellationToken);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при удалении оборудования с ID {ID}", id);
                throw;
            }
        }
    }
}
