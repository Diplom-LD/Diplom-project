using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ManagerApp.Services
{
    public class JsonService(ILogger<JsonService> _logger)
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public T? Deserialize<T>(string jsonContent)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(jsonContent, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error for type {Type}. Response: {JsonContent}", typeof(T), jsonContent);
                return default;
            }
        }
    }
}
