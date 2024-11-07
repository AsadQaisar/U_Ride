namespace U_Ride.Services
{
    public class RideService
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

        // Method to calculate distance between two latitude and longitude points in kilometers using Haversine formula
        private double CalculateDistanceInKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double EarthRadiusKm = 6371.0;

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
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

        // Method to check if the student's stop point is within any radius along the driver's route
        public bool IsPointWithinRouteRadius(string studentStopPoint, List<(double Latitude, double Longitude)> routePoints, double radiusKm)
        {
            var (studentLat, studentLon) = ParseCoordinates(studentStopPoint);

            foreach (var point in routePoints)
            {
                double distance = CalculateDistanceInKm(studentLat, studentLon, point.Latitude, point.Longitude);

                // Check if the student's stop point is within the radius of the route point
                if (distance <= radiusKm)
                {
                    return true;
                }
            }

            return false; // Return false if no points are within the radius
        }

        // Method to calculate estimated price based on distance and number of seats
        public double EstimatePrice(double BaseRatePerKm, double distanceKm, int availableSeats)
        {
            if (availableSeats <= 0)
            {
                throw new ArgumentException("Available seats must be greater than 0.");
            }

            // Calculate total cost based on distance and base rate
            double totalCost = distanceKm * BaseRatePerKm;

            // Divide total cost by the number of available seats for individual share
            double pricePerSeat = totalCost / availableSeats;

            // Optionally round the price per seat to two decimal places
            return Math.Round(pricePerSeat, 2);
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

        // Convert degrees to radians
        public double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}
