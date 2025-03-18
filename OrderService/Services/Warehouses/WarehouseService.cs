using System.Text.RegularExpressions;
using OrderService.Models.Warehouses;
using OrderService.Repositories.Warehouses;
using OrderService.Services.GeoLocation.GeoCodingClient;

namespace OrderService.Services.Warehouses
{
    public partial class WarehouseService(IStockRepository<Warehouse> repository, IGeoCodingService geoCodingService, ILogger<WarehouseService> logger)
        : BaseStockService<Warehouse>(repository, logger)
    {
        [GeneratedRegex(@"^\+?[1-9]\d{7,14}$")]
        private static partial Regex PhoneRegex();

        private readonly IGeoCodingService _geoCodingService = geoCodingService;

        /// <summary>
        /// Валидация склада перед сохранением.
        /// </summary>
        protected override void ValidateStockItem(Warehouse warehouse)
        {
            if (string.IsNullOrWhiteSpace(warehouse.Name))
                throw new ArgumentException("Название склада не может быть пустым.", nameof(warehouse));

            if (warehouse.Name.Length > 100)
                throw new ArgumentException("Название склада не может превышать 100 символов.", nameof(warehouse));

            if (string.IsNullOrWhiteSpace(warehouse.Address) || warehouse.Address.Length > 200)
                throw new ArgumentException("Адрес склада не может быть пустым и не должен превышать 200 символов.", nameof(warehouse));

            if (string.IsNullOrWhiteSpace(warehouse.ContactPerson) || warehouse.ContactPerson.Length > 50)
                throw new ArgumentException("Контактное лицо не может быть пустым и должно быть не длиннее 50 символов.", nameof(warehouse));

            if (string.IsNullOrWhiteSpace(warehouse.PhoneNumber) || !PhoneRegex().IsMatch(warehouse.PhoneNumber))
                throw new ArgumentException("Некорректный формат номера телефона.", nameof(warehouse));
        }

        /// <summary>
        /// Добавление нового склада с геокодингом.
        /// </summary>
        public override async Task<string> AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
        {
            ValidateStockItem(warehouse);

            if (await _repository.GetByNameAsync(warehouse.Name, cancellationToken) is not null)
                throw new ArgumentException($"Склад с названием '{warehouse.Name}' уже существует.", nameof(warehouse));

            var coordinates = await _geoCodingService.GetBestCoordinateAsync(warehouse.Address);

            if (coordinates is not null)
            {
                var (latitude, longitude, _) = coordinates.Value;
                warehouse.Latitude = latitude;
                warehouse.Longitude = longitude;
            }
            else
            {
                throw new ArgumentException($"Не удалось получить координаты для адреса: {warehouse.Address}", nameof(warehouse));
            }

            var id = await _repository.AddAsync(warehouse, cancellationToken);
            LogAction("Добавлен новый склад", warehouse, id);

            return id;
        }

        /// <summary>
        /// Обновление склада с учетом возможного изменения адреса.
        /// </summary>
        public override async Task<bool> UpdateAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
        {
            ValidateStockItem(warehouse);

            if (string.IsNullOrWhiteSpace(warehouse.ID.ToString()))
                throw new ArgumentException("ID склада не может быть пустым.", nameof(warehouse));

            var existingWarehouse = await _repository.GetByIdAsync(warehouse.ID.ToString(), cancellationToken)
                ?? throw new ArgumentException($"Склад с ID {warehouse.ID} не найден.", nameof(warehouse));

            if (warehouse.Name != existingWarehouse.Name &&
                await _repository.GetByNameAsync(warehouse.Name, cancellationToken) is not null)
            {
                throw new ArgumentException($"Склад с названием '{warehouse.Name}' уже существует.", nameof(warehouse));
            }

            if (!string.Equals(warehouse.Address, existingWarehouse.Address, StringComparison.OrdinalIgnoreCase))
            {
                var coordinates = await _geoCodingService.GetBestCoordinateAsync(warehouse.Address);

                if (coordinates is not null)
                {
                    var (latitude, longitude, _) = coordinates.Value;
                    warehouse.Latitude = latitude;
                    warehouse.Longitude = longitude;
                }
                else
                {
                    throw new ArgumentException($"Не удалось получить координаты для адреса: {warehouse.Address}", nameof(warehouse));
                }
            }
            else
            {
                warehouse.Latitude = existingWarehouse.Latitude;
                warehouse.Longitude = existingWarehouse.Longitude;
            }

            var updated = await _repository.UpdateAsync(warehouse, cancellationToken);
            if (updated)
                LogAction("Обновлен склад", warehouse, warehouse.ID.ToString());

            return updated;
        }

        /// <summary>
        /// Логирование действий с складами.
        /// </summary>
        protected override void LogAction(string action, Warehouse? item, string id)
        {
            _logger.LogInformation("{Action}: {WarehouseName} (ID: {Id})", action,
                item?.Name ?? "Неизвестный склад", id);
        }
    }
}
