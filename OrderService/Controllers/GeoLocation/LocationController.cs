using Microsoft.AspNetCore.Mvc;
using OrderService.Services.GeoLocation;

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
    /// 📦 Получение ближайшего склада по координатам.
    /// </summary>
    [HttpGet("nearest-warehouse")]
    public async Task<IActionResult> GetNearestWarehouse([FromQuery] double latitude, [FromQuery] double longitude)
    {
        if (latitude == 0 || longitude == 0)
            return BadRequest(new { message = "Необходимо указать корректные координаты." });

        _logger.LogInformation("📦 Поиск ближайшего склада для координат: {Latitude}, {Longitude}", latitude, longitude);

        var nearestWarehouse = await _nearestLocationFinderService.FindNearestWarehouseAsync(latitude, longitude);
        if (nearestWarehouse == null)
        {
            _logger.LogWarning("⚠️ Ближайший склад не найден для координат: {Latitude}, {Longitude}", latitude, longitude);
            return NotFound(new { message = "Ближайший склад не найден." });
        }

        return Ok(nearestWarehouse);
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

        var technicians = await _nearestLocationFinderService.FindTechniciansAsync(latitude, longitude, technicianIds, count);
        if (technicians.Count == 0)
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
        [FromQuery] List<string>? technicianIds = null,
        [FromQuery] int count = 2,
        [FromQuery] bool useTraffic = true)
    {
        if (latitude == 0 || longitude == 0)
            return BadRequest(new { message = "Необходимо указать корректные координаты." });

        _logger.LogInformation("🔍 Поиск ближайшего склада, {Count} техников и маршрутов для координат: {Latitude}, {Longitude}, с учетом пробок: {UseTraffic}",
            count, latitude, longitude, useTraffic);

        var result = await _nearestLocationFinderService.FindNearestLocationWithRoutesAsync(latitude, longitude, technicianIds, count);

        if (result.Routes.Count == 0)
        {
            _logger.LogWarning("⚠️ Не удалось построить маршруты до заявки.");
            return NotFound(new { message = "Не удалось построить маршруты до заявки." });
        }

        return Ok(result);
    }
}
