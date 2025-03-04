using OrderService.Models.Warehouses;
using OrderService.Repositories.Warehouses;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderService.Services.Warehouses
{
    public class EquipmentStockService(IEquipmentStockRepository repository, ILogger<EquipmentStockService> logger) : IEquipmentStockService
    {
        private readonly IEquipmentStockRepository _repository = repository;
        private readonly ILogger<EquipmentStockService> _logger = logger;

        public async Task<List<EquipmentStock>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _repository.GetAllAsync(cancellationToken);

        public async Task<EquipmentStock?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateId(id);
            return await _repository.GetByIdAsync(id, cancellationToken);
        }

        public async Task AddAsync(EquipmentStock equipment, CancellationToken cancellationToken = default)
        {
            ValidateEquipmentStock(equipment);
            await EnsureUniqueModelNameAsync(equipment.ModelName, cancellationToken);

            await _repository.AddAsync(equipment, cancellationToken);
            LogAction("Добавлено оборудование", equipment.ModelName, equipment.ID);
        }

        public async Task<bool> UpdateAsync(EquipmentStock equipment, CancellationToken cancellationToken = default)
        {
            ValidateEquipmentStock(equipment);
            ValidateId(equipment.ID);
            await EnsureUniqueModelNameAsync(equipment.ModelName, cancellationToken, equipment.ID);

            var updated = await _repository.UpdateAsync(equipment, cancellationToken);
            if (updated)
            {
                LogAction("Оборудование обновлено", equipment.ModelName, equipment.ID);
            }
            return updated;
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            ValidateId(id);
            var deleted = await _repository.DeleteAsync(id, cancellationToken);
            if (deleted)
            {
                LogAction("Оборудование удалено", null, id);
            }
            return deleted;
        }

        private static void ValidateId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID оборудования не может быть пустым.", nameof(id));
        }


        private static void ValidateEquipmentStock(EquipmentStock equipment)
        {
            if (equipment == null)
                throw new ArgumentNullException(nameof(equipment), "Оборудование не может быть null.");

            if (string.IsNullOrWhiteSpace(equipment.ModelName))
                throw new ArgumentException("Название модели не может быть пустым.", "equipment.ModelName");

            if (equipment.ModelName.Length > 50)
                throw new ArgumentException("Название модели не может превышать 50 символов.", "equipment.ModelName");

            if (equipment.Price <= 0)
                throw new ArgumentException("Цена должна быть больше 0.", "equipment.Price");

            if (equipment.Quantity < 0)
                throw new ArgumentException("Количество не может быть отрицательным.", "equipment.Quantity");

            if (equipment.BTU < 1000 || equipment.BTU > 300000)
                throw new ArgumentException("BTU должен быть в диапазоне от 1000 до 300000.", "equipment.BTU");

            if (equipment.ServiceArea <= 0)
                throw new ArgumentException("Площадь обслуживания должна быть больше 0.", "equipment.ServiceArea");
        }

        private async Task EnsureUniqueModelNameAsync(string modelName, CancellationToken cancellationToken, string? idToExclude = null)
        {
            if (await _repository.GetByModelNameAsync(modelName, cancellationToken) is { ID: var existingId } && existingId != idToExclude)
            {
                throw new ArgumentException($"Оборудование с названием '{modelName}' уже существует.", nameof(modelName));
            }
        }

        private void LogAction(string action, string? modelName, string id)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                _logger.LogInformation("{Action}: ID = {Id}", action, id);
            }
            else
            {
                _logger.LogInformation("{Action}: {ModelName} (ID: {Id})", action, modelName, id);
            }
        }

    }
}
