using System.Globalization;
using System.Text.Json.Serialization;

namespace OrderService.DTO.GeoLocation
{
    public class GeoLocationResponse
    {
        [JsonPropertyName("place_id")]
        public long PlaceId { get; set; }

        [JsonPropertyName("lat")]
        public required string Lat { get; set; }

        [JsonPropertyName("lon")]
        public required string Lon { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = "Unknown";

        [JsonPropertyName("importance")]
        public double Importance { get; set; }

        [JsonPropertyName("class")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Class { get; set; }

        [JsonPropertyName("type")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Type { get; set; }

        [JsonIgnore]
        public double Latitude => double.TryParse(Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ? lat : 0.0;

        [JsonIgnore]
        public double Longitude => double.TryParse(Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon) ? lon : 0.0;
    }
}