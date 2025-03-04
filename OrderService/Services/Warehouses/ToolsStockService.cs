using OrderService.Models.Warehouses;
using OrderService.Repositories.Warehouses;

namespace OrderService.Services.Warehouses
{
    public class ToolsStockService(IToolsStockRepository _repository) : IToolsStockService
    {
        public async Task<List<ToolsStock>> GetAllAsync() => await _repository.GetAllAsync();

        public async Task<ToolsStock?> GetByIdAsync(string id) => await _repository.GetByIdAsync(id);

        public async Task AddAsync(ToolsStock tool)
        {
            ValidateToolsStock(tool);

            if (await _repository.GetByToolNameAsync(tool.ToolName) is not null)
                throw new ArgumentException($"Инструмент с названием '{tool.ToolName}' уже существует.");

            await _repository.AddAsync(tool);
        }

        public async Task<bool> UpdateAsync(ToolsStock tool)
        {
            ValidateToolsStock(tool);

            if (string.IsNullOrWhiteSpace(tool.ID))
                throw new ArgumentException("ID инструмента не может быть пустым.");

            if (await _repository.GetByIdAsync(tool.ID) is null)
                throw new ArgumentException($"Инструмент с ID {tool.ID} не найден.");

            var duplicate = await _repository.GetByToolNameAsync(tool.ToolName);
            if (duplicate is not null && duplicate.ID != tool.ID)
                throw new ArgumentException($"Инструмент с названием '{tool.ToolName}' уже существует.");

            return await _repository.UpdateAsync(tool);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID инструмента не может быть пустым.");

            if (await _repository.GetByIdAsync(id) is null)
                return false;

            return await _repository.DeleteAsync(id);
        }

        private static void ValidateToolsStock(ToolsStock tool)
        {
            if (string.IsNullOrWhiteSpace(tool.ToolName))
                throw new ArgumentException("Название инструмента не может быть пустым.");

            if (tool.ToolName.Length > 50)
                throw new ArgumentException("Название инструмента не может превышать 50 символов.");

            if (tool.Quantity < 0)
                throw new ArgumentException("Количество не может быть отрицательным.");
        }
    }
}
