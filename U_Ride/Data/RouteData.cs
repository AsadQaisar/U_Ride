namespace U_Ride.Data
{
    public class Engine
    {
        public string version { get; set; }
        public DateTime build_date { get; set; }
        public DateTime graph_date { get; set; }
    }

    public class Metadata
    {
        public string attribution { get; set; }
        public string service { get; set; }
        public long timestamp { get; set; }
        public Query query { get; set; }
        public Engine engine { get; set; }
    }

    public class Query
    {
        public List<List<double>> coordinates { get; set; }
        public string profile { get; set; }
        public string format { get; set; }
    }

    public class RouteData
    {
        public List<double> bbox { get; set; }
        public List<Routes> routes { get; set; }
        public Metadata metadata { get; set; }
    }

    public class Routes
    {
        public Summary summary { get; set; }
        public List<double> bbox { get; set; }
        public string geometry { get; set; }
        public List<int> way_points { get; set; }
    }

    public class Summary
    {
        public double distance { get; set; }
        public double duration { get; set; }
    }

    public class PointInfo
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double DistanceFromEndpoint { get; set; }
    }

    public class RadiusResult
    {
        public List<PointInfo> PointsWithinRadius { get; set; } = new List<PointInfo>();
        public PointInfo ClosestPoint { get; set; }
    }
}

