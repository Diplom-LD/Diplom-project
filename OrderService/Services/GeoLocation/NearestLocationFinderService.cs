using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrderService.DTO.GeoLocation;
using OrderService.DTO.Technicians;
using OrderService.Models.Technicians;
using OrderService.Models.Warehouses;
using OrderService.Repositories.Technicians;
using OrderService.Services.Warehouses;

namespace OrderService.Services.GeoLocation
{
    public class NearestLocationFinderService(
        WarehouseService warehouseService, 
        TechnicianRedisRepository technicianRedisRepository,
        ILogger<NearestLocationFinderService> logger)
    {
        private readonly WarehouseService _warehouseService = warehouseService;
        private readonly TechnicianRedisRepository _technicianRedisRepository = technicianRedisRepository;
        private readonly ILogger<NearestLocationFinderService> _logger = logger;

        /// <summary>
        /// Определяет ближайший склад и список техников (по ID или ближайших).
        /// </summary>
        public async Task<(WarehouseDTO? NearestWarehouse, List<TechnicianDTO> SelectedTechnicians)>
            FindNearestLocationAsync(double latitude, double longitude, List<string>? requestedTechnicianIds = null, int technicianCount = 2)
        {
            _logger.LogInformation("Поиск ближайшего склада и {TechnicianCount} техников...", technicianCount);

            var warehouse = await FindNearestWarehouseAsync(latitude, longitude);
            var technicians = await FindTechniciansAsync(latitude, longitude, requestedTechnicianIds, technicianCount);

            return (warehouse, technicians);
        }

        /// <summary>
        /// Поиск ближайшего склада.
        /// </summary>
        public async Task<WarehouseDTO?> FindNearestWarehouseAsync(double latitude, double longitude)
        {
            _logger.LogInformation("Ищем ближайший склад для координат: {Latitude}, {Longitude}", latitude, longitude);

            var warehouses = await _warehouseService.GetAllAsync();
            if (warehouses == null || warehouses.Count == 0)
            {
                _logger.LogWarning("В системе нет складов!");
                return null;
            }

            var nearestWarehouse = warehouses
                .Select(w => new
                {
                    Warehouse = w,
                    Distance = DistanceCalculator.CalculateDistance(latitude, longitude, w.Latitude, w.Longitude)
                })
                .OrderBy(w => w.Distance)
                .FirstOrDefault();

            if (nearestWarehouse == null)
            {
                _logger.LogWarning("Ближайший склад не найден!");
                return null;
            }

            _logger.LogInformation("Ближайший склад: {WarehouseName} (расстояние: {Distance} км)",
                nearestWarehouse.Warehouse.Name, nearestWarehouse.Distance);

            return new WarehouseDTO
            {
                Id = nearestWarehouse.Warehouse.ID,
                Name = nearestWarehouse.Warehouse.Name,
                Latitude = nearestWarehouse.Warehouse.Latitude,
                Longitude = nearestWarehouse.Warehouse.Longitude
            };
        }

        /// <summary>
        /// Поиск ближайших доступных техников.
        /// </summary>
        public async Task<List<TechnicianDTO>> FindTechniciansAsync(double latitude, double longitude, List<string>? requestedTechnicianIds, int technicianCount)
        {
            _logger.LogInformation("Поиск ближайших техников (или по ID)");

            var technicians = await _technicianRedisRepository.GetAllAsync();
            if (technicians == null || technicians.Count == 0)
            {
                _logger.LogWarning("Нет доступных техников в системе!");
                return [];
            }

            var availableTechnicians = technicians.Where(t => t.IsAvailable).ToList();
            if (availableTechnicians.Count == 0)
            {
                _logger.LogWarning("Нет доступных техников!");
                return [];
            }

            List<Technician> selectedTechnicians;

            if (requestedTechnicianIds != null && requestedTechnicianIds.Count > 0)
            {
                selectedTechnicians = [.. availableTechnicians.Where(t => requestedTechnicianIds.Contains(t.Id))];
                _logger.LogInformation("Выбраны конкретные техники по ID: {Technicians}",
                    string.Join(", ", selectedTechnicians.Select(t => t.Id)));
            }
            else
            {
                selectedTechnicians = [.. availableTechnicians
                    .Select(t => new
                    {
                        Technician = t,
                        Distance = DistanceCalculator.CalculateDistance(latitude, longitude, t.Latitude, t.Longitude)
                    })
                    .OrderBy(t => t.Distance)
                    .Take(technicianCount)
                    .Select(t => t.Technician)];

                _logger.LogInformation("Выбраны {TechnicianCount} ближайших доступных техников", selectedTechnicians.Count);
            }

            return [.. selectedTechnicians.Select(t => new TechnicianDTO
            {
                Id = t.Id,
                FullName = t.FullName,
                Address = t.Address,
                Latitude = t.Latitude,
                Longitude = t.Longitude,
                IsAvailable = t.IsAvailable,
                CurrentOrderId = t.CurrentOrderId
            })];
        }
    }
}
