using System;
using System.Collections.Generic;

namespace FrontierDispatch.Core
{
    // Assignment Phase 5. Adapt shortest-path reasoning to a travel-cost budget.

    /// <summary>
    /// Finds locations that can be reached from a starting location without
    /// exceeding a maximum total travel cost.
    ///
    /// This is an adaptation of shortest-path reasoning: instead of searching
    /// for one goal, the query determines the cheapest known cost to every
    /// location that fits within the supplied budget.
    ///
    /// This class is intentionally incomplete in the assignment starter.
    ///
    /// What do we mean by "query"? In this context, a query is simply a one-time search for information.
    /// </summary>
    public sealed class ReachabilityQuery
    {
        private readonly WorldData _world;

        /// <summary>
        /// Creates a reachability query operating on the supplied world.
        /// </summary>
        public ReachabilityQuery(WorldData world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        /// <summary>
        /// Returns reachable location IDs mapped to their cheapest known
        /// travel cost from the starting location.
        ///
        /// Only locations whose cheapest cost is within maxTravelCost should
        /// be included. The starting location has cost 0.
        /// </summary>
        public IReadOnlyDictionary<int, int> FindReachable(int startLocationId, int maxTravelCost)
        {
            // TODO: adapt shortest-path reasoning to the travel-cost budget.
            //throw new NotImplementedException();

            Dictionary<int, int> reachable = new Dictionary<int, int>();

            // Validate start location exists
            if (_world.GetLocationById(startLocationId) == null || maxTravelCost < 0) {
                return reachable;
            }

            MinPriorityQueue<int> pq = new MinPriorityQueue<int>();

            reachable[startLocationId] = 0;
            pq.Enqueue(startLocationId, 0);

            while (pq.Count > 0) {
                int currentId = pq.Dequeue();
                int currentCost = reachable[currentId];

                // If the current cost already exceeds what we recorded as optimal, skip
                // (or if it exceeds maxTravelCost, we don't explore further from it)
                if (currentCost > maxTravelCost) {
                    continue;
                }

                // Explore outgoing connections using our optimized representation
                foreach (var connection in _world.GetOutgoing(currentId)) {
                    int neighborId = connection.ToLocationId;
                    int newCost = currentCost + connection.TravelCost;

                    if (newCost <= maxTravelCost) {
                        if (!reachable.TryGetValue(neighborId, out int existingCost) || newCost < existingCost) {
                            reachable[neighborId] = newCost;
                            pq.Enqueue(neighborId, newCost);
                        }
                    }
                }
            }

            return reachable;
        }
    }
}
