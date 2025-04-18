using System.Text.Json.Serialization;

namespace OrderService.DTO.GeoLocation
{
    public class GeoLocationResponse
    {
        [JsonPropertyName("place_id")]
        public long PlaceId { get; set; }

        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lon")]
        public double Longitude { get; set; }

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
    }
}
