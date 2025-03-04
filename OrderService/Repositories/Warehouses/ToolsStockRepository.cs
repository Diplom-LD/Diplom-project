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
    public class ToolsStockRepository(WarehouseMongoContext context, ILogger<ToolsStockRepository> logger)
        : IToolsStockRepository
    {
        private readonly IMongoCollection<ToolsStock> _toolsStock = context.ToolsStock;

        public async Task<List<ToolsStock>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _toolsStock.Find(_ => true).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении списка инструментов.");
                return [];
            }
        }

        public async Task<ToolsStock?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Передан некорректный ID инструмента.", nameof(id));

            try
            {
                return await _toolsStock.Find(t => t.ID == id)
                                        .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при поиске инструмента по ID {ID}", id);
                throw;
            }
        }

        public async Task<ToolsStock?> GetByToolNameAsync(string toolName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                throw new ArgumentException("Название инструмента не может быть пустым.", nameof(toolName));

            try
            {
                return await _toolsStock.Find(t => t.ToolName == toolName)
                                        .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при поиске инструмента по названию '{ToolName}'", toolName);
                throw;
            }
        }

        public async Task AddAsync(ToolsStock tool, CancellationToken cancellationToken = default)
        {
            if (tool is null)
                throw new ArgumentNullException(nameof(tool), "Передан null-объект инструмента.");

            try
            {
                await _toolsStock.InsertOneAsync(tool, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при добавлении инструмента.");
                throw;
            }
        }

        public async Task<bool> UpdateAsync(ToolsStock tool, CancellationToken cancellationToken = default)
        {
            if (tool is null)
                throw new ArgumentNullException(nameof(tool), "Передан null-объект инструмента.");

            if (string.IsNullOrWhiteSpace(tool.ID))
                throw new ArgumentException("Передан некорректный ID инструмента.", nameof(tool));

            try
            {
                var result = await _toolsStock.ReplaceOneAsync(
                    t => t.ID == tool.ID, tool, cancellationToken: cancellationToken);

                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обновлении инструмента с ID {ID}", tool.ID);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Передан некорректный ID инструмента.", nameof(id));

            try
            {
                var result = await _toolsStock.DeleteOneAsync(t => t.ID == id, cancellationToken);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при удалении инструмента с ID {ID}", id);
                throw;
            }
        }
    }
}
