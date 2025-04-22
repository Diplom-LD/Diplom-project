using System.Text.Json.Serialization;

namespace AuthService.Models.GeoCodingServices
{
    public class GeoLocationResponse
    {
        [JsonPropertyName("lat")]
        public string LatitudeRaw { get; set; } = string.Empty;

        [JsonPropertyName("lon")]
        public string LongitudeRaw { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonIgnore]
        public double Latitude => double.TryParse(LatitudeRaw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat) ? lat : 0;

        [JsonIgnore]
        public double Longitude => double.TryParse(LongitudeRaw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lon) ? lon : 0;
    }

}
