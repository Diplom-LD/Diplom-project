using Microsoft.AspNetCore.Mvc;
using OrderService.Services.Warehouses;

namespace OrderService.Controllers.Warehouses
{
    [ApiController]
    [Route("api/warehouse/{entityType}")]
    public class WarehouseController<T>(BaseStockService<T> stockService, ILogger<WarehouseController<T>> logger) : ControllerBase
    {
        private readonly BaseStockService<T> _stockService = stockService;
        private readonly ILogger<WarehouseController<T>> _logger = logger;

        /// <summary>
        /// Получить все элементы склада (материалы, инструменты, оборудование)
        /// </summary>
        [HttpGet("items")]
        public async Task<ActionResult<List<T>>> GetAllAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Запрос на получение всех элементов склада типа {EntityType}", typeof(T).Name);

            try
            {
                var items = await _stockService.GetAllAsync(cancellationToken);
                _logger.LogInformation("Получено {Count} элементов типа {EntityType}", items.Count, typeof(T).Name);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка элементов склада типа {EntityType}", typeof(T).Name);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получить элемент склада по ID
        /// </summary>
        [HttpGet("items/{id}")]
        public async Task<ActionResult<T>> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Запрос на получение элемента склада типа {EntityType} с ID {ID}", typeof(T).Name, id);

            try
            {
                var item = await _stockService.GetByIdAsync(id, cancellationToken);
                if (item == null)
                {
                    _logger.LogWarning("Элемент склада типа {EntityType} с ID {ID} не найден", typeof(T).Name, id);
                    return NotFound($"Элемент с ID {id} не найден.");
                }

                _logger.LogInformation("Успешно получен элемент склада типа {EntityType} с ID {ID}", typeof(T).Name, id);
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении элемента склада типа {EntityType} с ID {ID}", typeof(T).Name, id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Добавить новый элемент на склад
        /// </summary>
        [HttpPost("items")]
        public async Task<ActionResult<string>> AddAsync([FromBody] T item, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Запрос на добавление нового элемента склада типа {EntityType}", typeof(T).Name);

            try
            {
                var id = await _stockService.AddAsync(item, cancellationToken);
                _logger.LogInformation("Успешно добавлен элемент склада типа {EntityType} с ID {ID}", typeof(T).Name, id);
                return CreatedAtAction(nameof(GetByIdAsync), new { entityType = typeof(T).Name.ToLower(), id }, item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении элемента склада типа {EntityType}", typeof(T).Name);
                return StatusCode(500, "Ошибка при добавлении элемента");
            }
        }

        /// <summary>
        /// Обновить элемент склада
        /// </summary>
        [HttpPut("items/{id}")]
        public async Task<ActionResult> UpdateAsync(string id, [FromBody] T item, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Запрос на обновление элемента склада типа {EntityType} с ID {ID}", typeof(T).Name, id);

            try
            {
                var updated = await _stockService.UpdateAsync(item, cancellationToken);
                if (!updated)
                {
                    _logger.LogWarning("Не найден элемент склада типа {EntityType} с ID {ID} для обновления", typeof(T).Name, id);
                    return NotFound($"Элемент с ID {id} не найден.");
                }

                _logger.LogInformation("Элемент склада типа {EntityType} с ID {ID} успешно обновлен", typeof(T).Name, id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении элемента склада типа {EntityType} с ID {ID}", typeof(T).Name, id);
                return StatusCode(500, "Ошибка при обновлении элемента");
            }
        }

        /// <summary>
        /// Удалить элемент склада
        /// </summary>
        [HttpDelete("items/{id}")]
        public async Task<ActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Запрос на удаление элемента склада типа {EntityType} с ID {ID}", typeof(T).Name, id);

            try
            {
                var deleted = await _stockService.DeleteAsync(id, cancellationToken);
                if (!deleted)
                {
                    _logger.LogWarning("Не найден элемент склада типа {EntityType} с ID {ID} для удаления", typeof(T).Name, id);
                    return NotFound($"Элемент с ID {id} не найден.");
                }

                _logger.LogInformation("Элемент склада типа {EntityType} с ID {ID} успешно удален", typeof(T).Name, id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении элемента склада типа {EntityType} с ID {ID}", typeof(T).Name, id);
                return StatusCode(500, "Ошибка при удалении элемента");
            }
        }
    }
}
