using System.Globalization;
using System.Text.Json.Serialization;

namespace OrderService.DTO.GeoLocation
{
    public class GeoLocationResponse
    {
        [JsonPropertyName("place_id")]
        public long PlaceId { get; set; }

        [JsonPropertyName("lat")]
        public string LatitudeRaw { get; set; } = "0";

        [JsonPropertyName("lon")]
        public string LongitudeRaw { get; set; } = "0";

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
        public double Latitude => double.TryParse(LatitudeRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ? lat : 0;

        [JsonIgnore]
        public double Longitude => double.TryParse(LongitudeRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon) ? lon : 0;
    }
}
