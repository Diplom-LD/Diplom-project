namespace OrderService.Services.GeoLocation
{
    public static class DistanceCalculator
    {
        private const double EarthRadiusKm = 6371.0;

        /// <returns>Расстояние между точками в километрах</returns>
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = (lat2 - lat1) * (Math.PI / 180.0);
            double dLon = (lon2 - lon1) * (Math.PI / 180.0);

            double lat1Rad = lat1 * (Math.PI / 180.0);
            double lat2Rad = lat2 * (Math.PI / 180.0);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusKm * c;
        }
    }
}
