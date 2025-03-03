using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using ManagerApp.Clients;
using ManagerApp.Models.BTU;

namespace ManagerApp.Controllers.BTU
{
    [Authorize]
    public class BTUController(BTUCalcServiceClient _btuCalcClient, ILogger<BTUController> _logger) : Controller
    {
        public IActionResult BTUCalculator()
        {
            return View();
        }
        public IActionResult SearchConditioners()
        {
            return View("SearchConditioners");
        }

        [HttpPost]
        public async Task<IActionResult> Calculate([FromBody] BTURequestModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { Errors = errors });
            }

            _logger.LogInformation("Received BTU calculation request.");

            var result = await _btuCalcClient.CalculateBTUAsync(model);

            if (string.IsNullOrEmpty(result))
            {
                _logger.LogWarning("BTU calculation failed. No response from BTUCalcService.");
                return BadRequest("Ошибка при расчёте");
            }

            _logger.LogInformation("BTU calculation successful. Response: {Result}", result);
            return Content(result, "application/json");
        }

        [HttpGet("BTU/products/range")]
        public async Task<IActionResult> GetProductsByBTURange([FromQuery(Name = "btu_min")] int btuMin, [FromQuery(Name = "btu_max")] int btuMax)
        {
            _logger.LogInformation("Приняли запрос: btu_min={btuMin}, btu_max={btuMax}", btuMin, btuMax);

            if (btuMin > btuMax)
            {
                return BadRequest("Минимальное значение BTU не может быть больше максимального.");
            }

            var result = await _btuCalcClient.GetProductsByBTURangeAsync(btuMin, btuMax);

            if (string.IsNullOrEmpty(result))
            {
                _logger.LogWarning("Не нашли товары для диапазона: {btuMin} - {btuMax}", btuMin, btuMax);
                return NotFound("Товары не найдены");
            }

            _logger.LogInformation("Найдены товары: {result}", result);
            return Content(result, "application/json");
        }

        [HttpGet("BTU/products/btu/{btu}")]
        public async Task<IActionResult> GetProductsByExactBTU(int btu)
        {
            _logger.LogInformation("Fetching products with exact BTU: {BTU}", btu);
            var result = await _btuCalcClient.GetProductsByExactBTUAsync(btu);

            if (string.IsNullOrEmpty(result))
            {
                _logger.LogWarning("No products found for BTU: {BTU}", btu);
                return NotFound("Товары не найдены");
            }

            return Content(result, "application/json");
        }

        [HttpGet("BTU/products/extremes")]
        public async Task<IActionResult> GetExtremeBTUProducts()
        {
            _logger.LogInformation("Fetching products with extreme BTU values.");

            try
            {
                var result = await _btuCalcClient.GetExtremeBTUProductsAsync();

                if (string.IsNullOrEmpty(result))
                {
                    _logger.LogWarning("No extreme BTU products found.");
                    return NotFound(new { message = "Товары с минимальным и максимальным BTU не найдены" });
                }

                _logger.LogInformation("Extreme BTU products found.");
                return Content(result, "application/json");
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Network error while fetching extreme BTU products.");
                return StatusCode(502, new { message = "Ошибка сети при получении данных" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching extreme BTU products.");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("BTU/products/stores")]
        public async Task<IActionResult> GetStores()
        {
            _logger.LogInformation("Fetching list of stores");
            var result = await _btuCalcClient.GetStoresAsync();

            if (string.IsNullOrEmpty(result))
            {
                _logger.LogWarning("No stores found.");
                return NotFound("Магазины не найдены");
            }

            return Content(result, "application/json");
        }

        [HttpGet("BTU/products/store/{storeName}")]
        public async Task<IActionResult> GetProductsByStore(string storeName)
        {
            _logger.LogInformation("Fetching products from store: {StoreName}", storeName);
            var result = await _btuCalcClient.GetProductsByStoreAsync(storeName);

            if (string.IsNullOrEmpty(result))
            {
                _logger.LogWarning("No products found in store: {StoreName}", storeName);
                return NotFound($"Магазин {storeName} не найден или в нем нет товаров");
            }

            return Content(result, "application/json");
        }

        [HttpGet("BTU/products/service_area/{area}")]
        public async Task<IActionResult> GetProductsByServiceArea(int area)
        {
            _logger.LogInformation("Fetching products for service area: {Area}", area);

            try
            {
                var result = await _btuCalcClient.GetProductsByServiceAreaAsync(area);

                if (string.IsNullOrWhiteSpace(result) || result == "[]")  // Проверяем, если результат пустой JSON
                {
                    _logger.LogWarning("No products found for service area: {Area}", area);
                    return NotFound(new { message = "Товары не найдены" });
                }

                return Content(result, "application/json");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed for service area: {Area}", area);
                return StatusCode(502, new { message = "Ошибка соединения с BTUCalcService" }); // Ошибка прокси / соединения
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON response for service area: {Area}", area);
                return StatusCode(500, new { message = "Ошибка обработки данных с сервера" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching products for service area: {Area}", area);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("BTU/products/price/{price}")]
        public async Task<IActionResult> GetProductsByExactPrice(int price)
        {
            _logger.LogInformation("Fetching products with exact price: {Price}", price);
            var result = await _btuCalcClient.GetProductsByExactPriceAsync(price);

            if (string.IsNullOrEmpty(result))
            {
                _logger.LogWarning("No products found for price: {Price}", price);
                return NotFound("Товары не найдены");
            }

            return Content(result, "application/json");
        }

        [HttpGet("BTU/products/price")]
        public async Task<IActionResult> GetProductsByPriceRange([FromQuery] int price_min, [FromQuery] int price_max)
        {
            _logger.LogInformation("Fetching products in price range: {PriceMin} - {PriceMax}", price_min, price_max);

            if (price_min > price_max)
            {
                _logger.LogWarning("Invalid price range: {PriceMin} > {PriceMax}", price_min, price_max);
                return BadRequest(new { message = "Минимальная цена не может быть больше максимальной." });
            }

            try
            {
                var result = await _btuCalcClient.GetProductsByPriceRangeAsync(price_min, price_max);

                if (string.IsNullOrWhiteSpace(result) || result == "[]")
                {
                    _logger.LogWarning("No products found in price range: {PriceMin} - {PriceMax}", price_min, price_max);
                    return NotFound(new { message = "Товары не найдены в указанном диапазоне цен" });
                }

                return Content(result, "application/json");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed for price range: {PriceMin} - {PriceMax}", price_min, price_max);
                return StatusCode(502, new { message = "Ошибка соединения с BTUCalcService" });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON response for price range: {PriceMin} - {PriceMax}", price_min, price_max);
                return StatusCode(500, new { message = "Ошибка обработки данных с сервера" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching products for price range: {PriceMin} - {PriceMax}", price_min, price_max);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

    }
}
