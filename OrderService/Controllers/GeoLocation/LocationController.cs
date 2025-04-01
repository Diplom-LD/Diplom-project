using Microsoft.AspNetCore.Mvc;
using OrderService.Services.GeoLocation;
using OrderService.Services.GeoLocation.GeoCodingClient;

namespace OrderService.Controllers.GeoLocation;

[ApiController]
[Route("location")]
public class LocationController(
    IGeoCodingService geoCodingService,
    NearestLocationFinderService nearestLocationFinderService,
    ILogger<LocationController> logger) : ControllerBase
{
    private readonly IGeoCodingService _geoCodingService = geoCodingService;
    private readonly NearestLocationFinderService _nearestLocationFinderService = nearestLocationFinderService;
    private readonly ILogger<LocationController> _logger = logger;

    /// <summary>
    /// 📍 Получение координат по адресу.
    /// </summary>
    [HttpGet("coordinates")]
    public async Task<IActionResult> GetCoordinates([FromQuery] string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return BadRequest(new { message = "Адрес обязателен." });

        _logger.LogInformation("📍 Запрос координат для адреса: {Address}", address);

        var coordinates = await _geoCodingService.GetCoordinatesAsync(address);

        if (coordinates == null)
        {
            _logger.LogWarning("⚠️ Координаты не найдены для адреса: {Address}", address);
            return NotFound(new { message = "Координаты не найдены." });
        }

        return Ok(new
        {
            latitude = coordinates.Value.Latitude,
            longitude = coordinates.Value.Longitude,
            displayName = coordinates.Value.DisplayName
        });
    }

    /// <summary>
    /// 📍 Получение наиболее точных координат по адресу.
    /// </summary>
    [HttpGet("best-coordinates")]
    public async Task<IActionResult> GetBestCoordinates([FromQuery] string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return BadRequest(new { message = "Адрес обязателен." });

        _logger.LogInformation("📍 Запрос лучших координат для адреса: {Address}", address);

        var coordinates = await _geoCodingService.GetBestCoordinateAsync(address);

        if (!coordinates.HasValue)
        {
            _logger.LogWarning("⚠️ Лучшие координаты не найдены для адреса: {Address}", address);
            return NotFound(new { message = "Лучшие координаты не найдены." });
        }

        var (latitude, longitude, displayName) = coordinates.Value;

        if (latitude == 0 && longitude == 0)
        {
            _logger.LogWarning("⚠️ Получены нулевые координаты для адреса: {Address}", address);
            return NotFound(new { message = "Лучшие координаты не найдены." });
        }

        return Ok(new
        {
            latitude,
            longitude,
            displayName = string.IsNullOrWhiteSpace(displayName) ? "Неизвестный адрес" : displayName
        });
    }

    /// <summary>
    /// 👷 Получение ближайших техников по координатам.
    /// </summary>
    [HttpGet("nearest-technicians")]
    public async Task<IActionResult> GetNearestTechnicians(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] List<string>? technicianIds = null,
        [FromQuery] int count = 2)
    {
        if (latitude == 0 || longitude == 0)
            return BadRequest(new { message = "Необходимо указать корректные координаты." });

        _logger.LogInformation("👷 Поиск ближайших {Count} техников для координат: {Latitude}, {Longitude}", count, latitude, longitude);

        // Вызов метода, а не ссылка на метод
        var technicians = await _nearestLocationFinderService.FindTechniciansAsync(latitude, longitude, technicianIds);

        if (technicians == null || technicians.Count == 0)
        {
            _logger.LogWarning("⚠️ Ближайшие техники не найдены для координат: {Latitude}, {Longitude}", latitude, longitude);
            return NotFound(new { message = "Ближайшие техники не найдены." });
        }

        return Ok(technicians);
    }

    /// <summary>
    /// 🔍 Получение ближайшего склада, техников и маршрутов в одном запросе.
    /// </summary>
    [HttpGet("nearest-location-with-routes")]
    public async Task<IActionResult> GetNearestLocationWithRoutes(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] OrderService.Models.Enums.OrderType orderType,
        [FromQuery] List<string>? technicianIds = null,
        [FromQuery] int count = 2,
        [FromQuery] bool useTraffic = true)
    {
        if (latitude == 0 || longitude == 0)
            return BadRequest(new { message = "Необходимо указать корректные координаты." });

        _logger.LogInformation("🔍 Поиск ближайшего склада, {Count} техников и маршрутов для координат: {Latitude}, {Longitude}, с учетом пробок: {UseTraffic}",
            count, latitude, longitude, useTraffic);

        var result = await _nearestLocationFinderService.FindNearestLocationsAsync(
            latitude, longitude, orderType, null, technicianIds);

        if (result == null || result.Routes.Count == 0)
        {
            _logger.LogWarning("⚠️ Не удалось построить маршруты до заявки.");
            return NotFound(new { message = "Не удалось построить маршруты до заявки." });
        }

        return Ok(result);
    }


    /// <summary>
    /// 📦 Получение полной информации о координатах всех складов и домашних адресах техников.
    /// </summary>
    [HttpGet("all-locations")]
    public async Task<IActionResult> GetAllLocations()
    {
        try
        {
            var technicians = await _nearestLocationFinderService.GetAllTechnicianHomeLocationsAsync();
            var warehouses = await _nearestLocationFinderService.GetAllWarehouseLocationsAsync();

            return Ok(new
            {
                technicians = technicians.Select(t => new
                {
                    id = t.TechnicianId,
                    fullName = t.FullName,
                    email = t.Email,
                    address = t.Address,
                    phoneNumber = t.PhoneNumber,
                    latitude = t.Latitude,
                    longitude = t.Longitude
                }),
                warehouses = warehouses.Select(w => new
                {
                    id = w.WarehouseId,
                    name = w.Name,
                    address = w.Address,
                    contactPerson = w.ContactPerson,
                    phoneNumber = w.PhoneNumber,
                    latitude = w.Latitude,
                    longitude = w.Longitude
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при получении всех координат.");
            return StatusCode(500, new { message = "Не удалось получить координаты." });
        }
    }

}
