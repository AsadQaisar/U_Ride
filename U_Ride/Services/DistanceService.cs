namespace U_Ride.Services
{
    public class DistanceService
    {
        // Helper method to calculate distance between two points using Haversine formula
        public double CalculateDistance(string startPoint, string stopPoint)
        {
            var (startLat, startLon) = ParseCoordinates(startPoint);
            var (stopLat, stopLon) = ParseCoordinates(stopPoint);

            const double EarthRadiusKm = 6371.0;

            var dLat = DegreesToRadians(stopLat - startLat);
            var dLon = DegreesToRadians(stopLon - startLon);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(startLat)) * Math.Cos(DegreesToRadians(stopLat)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }

        // Helper method to divide route into interval points
        // double startLat, double startLon, double stopLat, double stopLon, int intervals

        public List<(double Latitude, double Longitude)> CalculateIntervalPoints(string startPoint, string stopPoint, int intervals)
        {
            var (startLat, startLon) = ParseCoordinates(startPoint);
            var (stopLat, stopLon) = ParseCoordinates(stopPoint);

            var intervalPoints = new List<(double Latitude, double Longitude)>();

            for (int i = 1; i <= intervals; i++)
            {
                var lat = startLat + (stopLat - startLat) * (i / (double)(intervals + 1));
                var lon = startLon + (stopLon - startLon) * (i / (double)(intervals + 1));
                intervalPoints.Add((lat, lon));
            }

            return intervalPoints;
        }

        // Convert degrees to radians
        public double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        // Parsing Lat and Long from string
        public (double Latitude, double Longitude) ParseCoordinates(string coordinates)
        {
            // Remove the parentheses and split the string by the comma
            var cleanedCoordinates = coordinates.Trim('(', ')');
            var parts = cleanedCoordinates.Split(',');

            if (parts.Length == 2)
            {
                // Try to parse the latitude and longitude values
                if (double.TryParse(parts[0].Trim(), out double latitude) && double.TryParse(parts[1].Trim(), out double longitude))
                {
                    return (latitude, longitude); // Return as tuple
                }
            }

            throw new FormatException("Invalid coordinate format.");
        }

    }
}
