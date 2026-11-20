using System;
using System.Collections.Generic;

namespace FrontierDispatch.Core
{
    // Assignment Phase 4.

    /// <summary>
    /// Finds minimum-total-cost routes through a Frontier Dispatch world
    /// using Dijkstra's shortest-path algorithm.
    ///
    /// This class is intentionally incomplete in the assignment starter.
    /// It should eventually use the student's prepared world representation
    /// and MinPriorityQueue implementation.
    /// </summary>
    public sealed class DijkstraRouteFinder
    {
        private readonly WorldData _world;

        /// <summary>
        /// Constructor. Creates a route finder operating on the supplied world.
        /// </summary>
        public DijkstraRouteFinder(WorldData world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        /// <summary>
        /// Finds a minimum-total-travel-cost route from the start location
        /// to the goal location.
        ///
        /// A successful RouteResult should contain the route as location IDs,
        /// including both the start and goal locations.
        /// </summary>
        public RouteResult FindRoute(int startLocationId, int goalLocationId)
        {
            // TODO: implement Dijkstra using your prepared world representation
            // and MinPriorityQueue<T>.
            //throw new NotImplementedException();

            // Edge case: If start and goal are the same, return zero-cost route immediately
            if (startLocationId == goalLocationId) {
                // Verify if the location actually exists in the world
                if (_world.GetLocationById(startLocationId) == null) {
                    return new RouteResult(false, Array.Empty<int>(), 0, 0);
                }

                return new RouteResult(true, new[]{startLocationId}, 0, 0);
            }

            MinPriorityQueue<int> pq = new MinPriorityQueue<int>();
            Dictionary<int, int> distances = new Dictionary<int, int>();
            Dictionary<int, int> previous = new Dictionary<int, int>();
            HashSet<int> visited = new HashSet<int>();

            distances[startLocationId] = 0;
            pq.Enqueue(startLocationId, 0);

            int expandedCount = 0;
            bool found = false;

            while (pq.Count > 0) {
                int currentId = pq.Dequeue();

                // If this node has already been processed with a shorter or equal distance, skip it entirely
                if (!visited.Add(currentId)) {
                    continue;
                }

                expandedCount++;

                // If the algorithm has reached the goal, terminate early
                if (currentId == goalLocationId) {
                    found = true;
                    break;
                }

                int currentDist = distances[currentId];

                // Iterate through outgoing connections using the new optimized WorldData function
                var outgoing = _world.GetOutgoing(currentId);
                foreach (var connection in outgoing) {
                    int neighborId = connection.ToLocationId;
                    int newDist = currentDist + connection.TravelCost;

                    if (!distances.TryGetValue(neighborId, out int existingDist) || newDist < existingDist)
                    {
                        distances[neighborId] = newDist;
                        previous[neighborId] = currentId;
                        pq.Enqueue(neighborId, newDist);
                    }
                }
            }

            if (!found) {
                return new RouteResult(false, Array.Empty<int>(), int.MaxValue, expandedCount);
            }

            // Reconstruct path from goal back to start
            var path = new List<int>();
            int curr = goalLocationId;
            while (curr != startLocationId) {
                path.Add(curr);
                curr = previous[curr];
            }
            path.Add(startLocationId);
            path.Reverse(); // needs to be reversed

            return new RouteResult(true, path, distances[goalLocationId], expandedCount);
        }
    }
}
