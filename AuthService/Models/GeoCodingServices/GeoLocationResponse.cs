using System.Text.Json.Serialization;
using System.Globalization;

namespace AuthService.Models.GeoCodingServices
{
    public class GeoLocationResponse
    {
        [JsonPropertyName("lat")]
        public string LatitudeString { get; set; } = string.Empty;

        [JsonPropertyName("lon")]
        public string LongitudeString { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonIgnore]
        public double Latitude => double.TryParse(LatitudeString, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) ? lat : 0;

        [JsonIgnore]
        public double Longitude => double.TryParse(LongitudeString, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon) ? lon : 0;
    }
}
