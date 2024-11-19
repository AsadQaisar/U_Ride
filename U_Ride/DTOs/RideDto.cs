using Azure.Core.GeoJson;

namespace U_Ride.DTOs
{
    public class RideDto
    {
        // Get Cordinates in Latitude and Longitude
        public class CoordinatesDto
        {
            public double StartLatitude { get; set; } // Latitude for the starting point

            public double StartLongitude { get; set; } // Longitude for the starting point
            
            public double StopLatitude { get; set; } // Latitude for the stopping point
            
            public double StopLongitude { get; set; } // Longitude for the stopping point
        }

        public class GeoCoordinatesDto
        {
            public string StartPoint { get; set; } // Expected format: "latitude, longitude"

            public string EndPoint { get; set; }  // Expected format: "latitude, longitude"

            public string? EncodedPolyline { get; set; }
        }

        public class RouteInfo
        {
            public double Distance { get; set; }
            
            public double Duration { get; set; }
            
            public List<double> BoundingBox { get; set; }
            
            public string Geometry { get; set; }
            
            public List<int> WayPoints { get; set; }
        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class DriverInfo
        {
            public string FullName { get; set; }

            public string Gender { get; set; }
            
            public string PhoneNumber { get; set; }
        }
        public class RideInfo
        {
            public int RouteMatched { get; set; }

            public string? Price { get; set; }

            public string AvailableSeats { get; set; }
        }
        public class VehicleInfo
        {
            public string VehicleType { get; set; }

            public string Make_Model { get; set; }

            public string LicensePlate { get; set; }
        }
        public class Root
        {
            public DriverInfo DriverInfo { get; set; }
           
            public RideInfo RideInfo { get; set; }
            
            public VehicleInfo VehicleInfo { get; set; }
        }

    }
}
