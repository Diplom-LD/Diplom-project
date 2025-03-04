using OrderService.Models.Warehouses;
using OrderService.Repositories.Warehouses;

namespace OrderService.Services.Warehouses
{
    public class MaterialsStockService(IMaterialsStockRepository repository) : IMaterialsStockService
    {
        private readonly IMaterialsStockRepository _repository = repository;

        public async Task<List<MaterialsStock>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<MaterialsStock?> GetByIdAsync(string id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task AddAsync(MaterialsStock material)
        {
            ValidateMaterialsStock(material);

            if (await _repository.GetByMaterialNameAsync(material.MaterialName) is not null)
                throw new ArgumentException($"Материал с названием '{material.MaterialName}' уже существует.");

            await _repository.AddAsync(material);
        }

        public async Task<bool> UpdateAsync(MaterialsStock material)
        {
            ValidateMaterialsStock(material);

            if (string.IsNullOrWhiteSpace(material.ID))
                throw new ArgumentException("ID материала не может быть пустым.");

            if (await _repository.GetByIdAsync(material.ID) is null)
                throw new ArgumentException($"Материал с ID {material.ID} не найден.");

            var duplicate = await _repository.GetByMaterialNameAsync(material.MaterialName);
            if (duplicate is not null && duplicate.ID != material.ID)
                throw new ArgumentException($"Материал с названием '{material.MaterialName}' уже существует.");

            return await _repository.UpdateAsync(material);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID материала не может быть пустым.");

            if (await _repository.GetByIdAsync(id) is null)
                return false;

            return await _repository.DeleteAsync(id);
        }

        private static void ValidateMaterialsStock(MaterialsStock material)
        {
            if (string.IsNullOrWhiteSpace(material.MaterialName))
                throw new ArgumentException("Название материала не может быть пустым.");

            if (material.MaterialName.Length > 50)
                throw new ArgumentException("Название материала не может превышать 50 символов.");

            if (material.Price <= 0)
                throw new ArgumentException("Цена должна быть больше 0.");

            if (material.Quantity < 0)
                throw new ArgumentException("Количество не может быть отрицательным.");
        }
    }
}
