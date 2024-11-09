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
        }

        public class RouteInfo
        {
            public double Distance { get; set; }
            
            public double Duration { get; set; }
            
            public List<double> BoundingBox { get; set; }
            
            public string Geometry { get; set; }
            
            public List<int> WayPoints { get; set; }
        }

    }
}
