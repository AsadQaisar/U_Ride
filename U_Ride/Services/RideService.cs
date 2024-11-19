using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http.Json;
using Newtonsoft.Json;
using U_Ride.Data;
using System.Text;

namespace U_Ride.Services
{
    public class RideService
    {
        // Generate Route Info From Start and End Coordinates Using External Service API
        public async Task<Routes> CalculateRouteDistanceAsync(string startPoint, string stopPoint)
        {
            Data.RouteData myDeserializedClass = null;
            var (startLat, startLon) = await ParseCoordinates(startPoint);
            var (stopLat, stopLon) = await ParseCoordinates(stopPoint);

            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://api.openrouteservice.org/");
            client.DefaultRequestHeaders.Add("Authorization", "5b3ce3597851110001cf6248c5d5515ab68c4a2d80976dabe92b7787");

            var routeRequest = new
            {
                coordinates = new[] { new[] { startLon, startLat }, new[] { stopLon, stopLat } },
                instructions = false
            };

            var response = await client.PostAsJsonAsync("v2/directions/driving-car", routeRequest);
            //response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                myDeserializedClass = JsonConvert.DeserializeObject<Data.RouteData>(jsonResponse);
            }
            return myDeserializedClass?.routes?.FirstOrDefault() ?? null;
        }

        // Decode Polyline
        public List<(double Lat, double Lon)> DecodePolyline(string encodedPolyline)
        {
            var polylinePoints = new List<(double Lat, double Lon)>();
            int index = 0, len = encodedPolyline.Length;
            int lat = 0, lon = 0;

            while (index < len)
            {
                int result = 1, shift = 0, b;
                do
                {
                    b = encodedPolyline[index++] - 63 - 1;
                    result += b << shift;
                    shift += 5;
                } while (b >= 0x1f);
                lat += (result & 1) != 0 ? ~(result >> 1) : (result >> 1);

                result = 1;
                shift = 0;
                do
                {
                    b = encodedPolyline[index++] - 63 - 1;
                    result += b << shift;
                    shift += 5;
                } while (b >= 0x1f);
                lon += (result & 1) != 0 ? ~(result >> 1) : (result >> 1);

                polylinePoints.Add((lat * 1e-5, lon * 1e-5));
            }

            return polylinePoints;
        }

        // Encode Polyline
        public string EncodePolyline(List<(double Lat, double Lon)> coordinates)
        {
            var encoded = new StringBuilder();
            int prevLat = 0, prevLon = 0;

            foreach (var (lat, lon) in coordinates)
            {
                int currentLat = (int)Math.Round(lat * 1e5);
                int currentLon = (int)Math.Round(lon * 1e5);

                int deltaLat = currentLat - prevLat;
                int deltaLon = currentLon - prevLon;

                encoded.Append(EncodeSignedValue(deltaLat));
                encoded.Append(EncodeSignedValue(deltaLon));

                prevLat = currentLat;
                prevLon = currentLon;
            }

            return encoded.ToString();
        }
        private string EncodeSignedValue(int value)
        {
            int sgnNum = value << 1;
            if (value < 0)
            {
                sgnNum = ~sgnNum;
            }
            return EncodeUnsignedValue(sgnNum);
        }
        private string EncodeUnsignedValue(int value)
        {
            var encoded = new StringBuilder();
            while (value >= 0x20)
            {
                encoded.Append((char)((0x20 | (value & 0x1f)) + 63));
                value >>= 5;
            }
            encoded.Append((char)(value + 63));
            return encoded.ToString();
        }

        // Find the Student's End Point if it lies within n'th KM of Driver's route (APPROACH # 1)
        public ((double Lat, double Lon)? Point, double Distance)? GetClosestPointWithinRadius(List<(double Lat, double Lon)> points,(double Lat, double Lon) endPoint,double radiusKm)
        {
            (double Lat, double Lon)? closestPoint = null;
            double minDistance = double.MaxValue;

            foreach (var point in points)
            {
                var distance = CalculateDistanceInKm(point.Lat, point.Lon, endPoint.Lat, endPoint.Lon);

                // Check if the point is within the radius and closer than the current minimum distance
                if (distance <= radiusKm && distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = point;
                }
            }

            return closestPoint != null ? ((Lat: closestPoint.Value.Lat, Lon: closestPoint.Value.Lon), minDistance) : null;
        }

        // Find the Student's End Point if it lies within n'th KM of Driver's route (APPROACH # 2)
        public RadiusResult GetPointsWithinRadiusAndClosest(List<(double Lat, double Lon)> points, (double Lat, double Lon) endPoint, double radiusKm)
        {
            var result = new RadiusResult();
            double minDistance = double.MaxValue;

            foreach (var point in points)
            {
                var distance = CalculateDistanceInKm(point.Lat, point.Lon, endPoint.Lat, endPoint.Lon);

                // If the point is within the radius, add it to the PointsWithinRadius list
                if (distance <= radiusKm)
                {
                    result.PointsWithinRadius.Add(new PointInfo
                    {
                        Latitude = point.Lat,
                        Longitude = point.Lon,
                        DistanceFromEndpoint = distance
                    });
                }

                // Check if this point is the closest one
                if (distance < minDistance)
                {
                    minDistance = distance;
                    result.ClosestPoint = new PointInfo
                    {
                        Latitude = point.Lat,
                        Longitude = point.Lon,
                        DistanceFromEndpoint = distance
                    };
                }
            }

            return result;
        }

        // Geo Route From Decoded Polyline Using External API
        public async Task<string> GetRouteGeoJsonAsync(List<(double Lat, double Lon)> coordinates)
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://api.openrouteservice.org/");
            client.DefaultRequestHeaders.Add("Authorization", "5b3ce3597851110001cf6248c5d5515ab68c4a2d80976dabe92b7787");

            // Convert the list of coordinates to the expected format
            var routeRequest = new
            {
                coordinates = coordinates.Select(c => new[] { c.Lon, c.Lat }).ToArray()
            };

            // Send the request to the /geojson endpoint
            var response = await client.PostAsJsonAsync("v2/directions/driving-car/geojson", routeRequest);
            response.EnsureSuccessStatusCode();

            // Return the GeoJSON string
            return await response.Content.ReadAsStringAsync();
        }

        // Helper method to calculate distance between two points using Haversine formula
        public async Task<double> CalculateDistance(string startPoint, string stopPoint)
        {
            var (startLat, startLon) = await ParseCoordinates(startPoint);
            var (stopLat, stopLon) = await ParseCoordinates(stopPoint);

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

        // Points on Route
        public List<(double Lat, double Lon)> GetIntermediatePoints(List<(double Lat, double Lon)> routePoints, double totalDistance, int numPoints)
        {
            var points = new List<(double Lat, double Lon)>();
            double segmentDistance = totalDistance / (numPoints - 1);

            double accumulatedDistance = 0;
            points.Add(routePoints[0]); // Start point

            for (int i = 1; i < routePoints.Count && points.Count < numPoints; i++)
            {
                var (prevLat, prevLon) = routePoints[i - 1];
                var (currentLat, currentLon) = routePoints[i];

                double segment = CalculateDistanceInKm(prevLat, prevLon, currentLat, currentLon);

                accumulatedDistance += segment;

                if (accumulatedDistance >= segmentDistance)
                {
                    double excess = accumulatedDistance - segmentDistance;
                    double ratio = (segment - excess) / segment;

                    double interpolatedLat = prevLat + ratio * (currentLat - prevLat);
                    double interpolatedLon = prevLon + ratio * (currentLon - prevLon);

                    points.Add((interpolatedLat, interpolatedLon));

                    accumulatedDistance = excess;
                }
            }

            points.Add(routePoints.Last()); // End point
            return points;
        }


        // Helper method to divide route into interval points
        // double startLat, double startLon, double stopLat, double stopLon, int intervals
        public async Task<List<(double Latitude, double Longitude)>> CalculateIntervalPoints(string startPoint, string stopPoint, int intervals)
        {
            var (startLat, startLon) = await ParseCoordinates(startPoint);
            var (stopLat, stopLon) = await ParseCoordinates(stopPoint);

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
        public async Task<bool> IsPointWithinRouteRadius(string studentStopPoint, List<(double Latitude, double Longitude)> routePoints, double radiusKm)
        {
            var (studentLat, studentLon) = await ParseCoordinates(studentStopPoint);

            foreach (var point in routePoints)
            {
                double distance = CalculateDistanceInKm(studentLat, studentLon, point.Latitude, point.Longitude);

                // Check if the student's stop point is within the radius of the route point
                if (distance <= radiusKm)
                {
                    return true;
                }
            }

            return false;
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
        public async Task<(double Latitude, double Longitude)> ParseCoordinates(string coordinates)
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
