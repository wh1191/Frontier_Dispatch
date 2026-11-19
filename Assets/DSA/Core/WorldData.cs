using System;
using System.Collections.Generic;
using System.Linq;

namespace FrontierDispatch.Core
{
    public sealed class WorldData
    {
        private readonly Location[] _locations;
        private readonly Connection[] _connections;

        private readonly Dictionary<int, Location> _locationMap;
        private readonly Dictionary<int, List<Connection>> _outgoingConnections;

        public WorldData(IEnumerable<Location> locations, IEnumerable<Connection> connections)
        {
            if (locations == null) throw new ArgumentNullException(nameof(locations));
            if (connections == null) throw new ArgumentNullException(nameof(connections));
            _locations = new List<Location>(locations).ToArray();
            _connections = new List<Connection>(connections).ToArray();

            // Map location IDs directly to Location objects for O(1) lookup
            _locationMap = _locations.ToDictionary(loc => loc.Id);

            // Pre-group outgoing connections for O(1) access
            _outgoingConnections = new Dictionary<int, List<Connection>>(); 
            foreach (var connection in _connections) {
                if (!_outgoingConnections.TryGetValue(connection.FromLocationId, out var list)) {
                    list = new List<Connection>();
                    _outgoingConnections[connection.FromLocationId] = list;
                }
                list.Add(connection);
            }
        }

        public IReadOnlyList<Location> Locations => _locations;
        public IReadOnlyList<Connection> Connections => _connections;

        public Location? GetLocationById(int id) {
            if (_locationMap.TryGetValue(id, out var location)) {
                return location;
            }
            return null;
        }

        public IReadOnlyList<Connection> GetOutgoing(int locationId) {
            if (_outgoingConnections.TryGetValue(locationId, out var list)) {
                return list;
            }
            return Array.Empty<Connection>();
        }
    }
}
