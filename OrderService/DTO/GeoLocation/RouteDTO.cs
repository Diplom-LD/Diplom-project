using System.Text.Json.Serialization;

namespace OrderService.DTO.GeoLocation
{
    public class RouteDTO
    {
        public Guid TechnicianId { get; set; } 
        public string TechnicianName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public double Distance { get; set; }
        public double Duration { get; set; }

        public List<RoutePoint> RoutePoints { get; set; } = [];
        public bool IsViaWarehouse { get; set; }
    }

    public class ORSRouteResponse
    {
        [JsonPropertyName("routes")]
        public List<ORSRoute> Routes { get; set; } = [];
    }

    public class ORSRoute
    {
        [JsonPropertyName("summary")]
        public ORSSummary Summary { get; set; } = new();

        [JsonPropertyName("geometry")]
        public string EncodedPolyline { get; set; } = string.Empty;

        public List<RoutePoint> GetCoordinates()
        {
            return !string.IsNullOrEmpty(EncodedPolyline) ? DecodePolyline(EncodedPolyline) : [];
        }

        private static List<RoutePoint> DecodePolyline(string encoded)
        {
            var polyline = new List<RoutePoint>();
            if (string.IsNullOrEmpty(encoded)) return polyline;

            int index = 0, len = encoded.Length;
            int lat = 0, lng = 0;

            while (index < len)
            {
                int b, shift = 0, result = 0;
                do
                {
                    b = encoded[index++] - 63;
                    result |= (b & 0x1F) << shift;
                    shift += 5;
                } while (b >= 0x20);
                int dlat = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lat += dlat;

                shift = 0;
                result = 0;
                do
                {
                    b = encoded[index++] - 63;
                    result |= (b & 0x1F) << shift;
                    shift += 5;
                } while (b >= 0x20);
                int dlng = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lng += dlng;

                polyline.Add(new RoutePoint(lat / 1E5, lng / 1E5));
            }

            return polyline;
        }
    }

    public class ORSSummary
    {
        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }
    }

    public class RoutePoint(double latitude, double longitude, bool isStopPoint = false)
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; } = latitude;

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; } = longitude;

        [JsonPropertyName("isStopPoint")]
        public bool IsStopPoint { get; set; } = isStopPoint;
    }
}
