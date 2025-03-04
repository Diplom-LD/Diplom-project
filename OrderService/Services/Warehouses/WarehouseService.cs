using System.Text.RegularExpressions;
using OrderService.Models.Warehouses;
using OrderService.Repositories.Warehouses;

namespace OrderService.Services.Warehouses
{
    public partial class WarehouseService(IWarehouseRepository _repository) : IWarehouseService
    {
        [GeneratedRegex(@"^\+?[1-9]\d{7,14}$")]
        private static partial Regex PhoneRegex();

        public async Task<List<Warehouse>> GetAllAsync() => await _repository.GetAllAsync();

        public async Task<Warehouse?> GetByIdAsync(string id) => await _repository.GetByIdAsync(id);

        public async Task CreateAsync(Warehouse warehouse)
        {
            ValidateWarehouse(warehouse);

            if (await _repository.GetByNameAsync(warehouse.Name) is not null)
                throw new ArgumentException($"Склад с названием '{warehouse.Name}' уже существует.");

            await _repository.CreateAsync(warehouse);
        }

        public async Task<bool> UpdateAsync(Warehouse warehouse)
        {
            ValidateWarehouse(warehouse);

            if (string.IsNullOrWhiteSpace(warehouse.ID))
                throw new ArgumentException("ID склада не может быть пустым.");

            if (await _repository.GetByIdAsync(warehouse.ID) is null)
                throw new ArgumentException($"Склад с ID {warehouse.ID} не найден.");

            var duplicate = await _repository.GetByNameAsync(warehouse.Name);
            if (duplicate is not null && duplicate.ID != warehouse.ID)
                throw new ArgumentException($"Склад с названием '{warehouse.Name}' уже существует.");

            return await _repository.UpdateAsync(warehouse);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID склада не может быть пустым.");

            if (await _repository.GetByIdAsync(id) is null)
                return false;

            return await _repository.DeleteAsync(id);
        }

        private static void ValidateWarehouse(Warehouse warehouse)
        {
            if (string.IsNullOrWhiteSpace(warehouse.Name))
                throw new ArgumentException("Название склада не может быть пустым.");

            if (warehouse.Name.Length > 100)
                throw new ArgumentException("Название склада не может превышать 100 символов.");

            if (string.IsNullOrWhiteSpace(warehouse.Address))
                throw new ArgumentException("Адрес склада не может быть пустым.");

            if (warehouse.Address.Length > 200)
                throw new ArgumentException("Адрес склада не может превышать 200 символов.");

            if (string.IsNullOrWhiteSpace(warehouse.ContactPerson))
                throw new ArgumentException("Контактное лицо не может быть пустым.");

            if (warehouse.ContactPerson.Length > 50)
                throw new ArgumentException("Имя контактного лица не может превышать 50 символов.");

            if (string.IsNullOrWhiteSpace(warehouse.PhoneNumber))
                throw new ArgumentException("Телефон склада не может быть пустым.");

            if (!PhoneRegex().IsMatch(warehouse.PhoneNumber))
                throw new ArgumentException("Некорректный формат номера телефона.");
        }
    }
}
