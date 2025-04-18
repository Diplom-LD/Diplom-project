using System.Text.Json.Serialization;

namespace AuthService.Models.GeoCodingServices
{
    public class GeoLocationResponse
    {
        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lon")]
        public double Longitude { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;
    }
}
