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
    public class MaterialsStockRepository(WarehouseMongoContext context, ILogger<MaterialsStockRepository> logger)
        : IMaterialsStockRepository
    {
        private readonly IMongoCollection<MaterialsStock> _materialsStock = context.MaterialsStock;

        public async Task<List<MaterialsStock>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _materialsStock.Find(_ => true).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении списка материалов.");
                return [];
            }
        }

        public async Task<MaterialsStock?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Передан некорректный ID материала.", nameof(id));

            try
            {
                return await _materialsStock.Find(m => m.ID == id)
                                            .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при поиске материала по ID {ID}", id);
                throw;
            }
        }

        public async Task<MaterialsStock?> GetByMaterialNameAsync(string materialName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(materialName))
                throw new ArgumentException("Название материала не может быть пустым.", nameof(materialName));

            try
            {
                return await _materialsStock.Find(m => m.MaterialName == materialName)
                                            .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при поиске материала по названию '{MaterialName}'", materialName);
                throw;
            }
        }

        public async Task AddAsync(MaterialsStock material, CancellationToken cancellationToken = default)
        {
            if (material is null)
                throw new ArgumentNullException(nameof(material), "Передан null-объект материала.");

            try
            {
                await _materialsStock.InsertOneAsync(material, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при добавлении материала.");
                throw;
            }
        }

        public async Task<bool> UpdateAsync(MaterialsStock material, CancellationToken cancellationToken = default)
        {
            if (material is null)
                throw new ArgumentNullException(nameof(material), "Передан null-объект материала.");

            if (string.IsNullOrWhiteSpace(material.ID))
                throw new ArgumentException("Передан некорректный ID материала.", nameof(material));

            try
            {
                var result = await _materialsStock.ReplaceOneAsync(
                    m => m.ID == material.ID, material, cancellationToken: cancellationToken);

                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обновлении материала с ID {ID}", material.ID);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Передан некорректный ID материала.", nameof(id));

            try
            {
                var result = await _materialsStock.DeleteOneAsync(m => m.ID == id, cancellationToken);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при удалении материала с ID {ID}", id);
                throw;
            }
        }
    }
}
