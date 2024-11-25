using System.Collections.Concurrent;
using U_Ride.Models;

namespace U_Ride.Data
{
    public class SharedDb
    {
        private readonly ConcurrentDictionary<string, UserConnection> _connections = new();

        public ConcurrentDictionary<string, UserConnection> connections => _connections;
    }
}
