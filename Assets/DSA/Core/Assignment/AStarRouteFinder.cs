using System;
using System.Collections.Generic;

namespace FrontierDispatch.Core
{
    /**
     * VG Extension: Finds minimum-total-cost routes using A* search 
     * with support for traversal constraints and heuristic distance estimation.
     */
    public sealed class AStarRouteFinder {
        private readonly WorldData _world;

        public AStarRouteFinder(WorldData world) {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        public RouteResult FindRoute(int startLocationId, int goalLocationId, Func<int, bool>? isBlocked = null) {
            if (startLocationId == goalLocationId) {
                if (_world.GetLocationById(startLocationId) == null)
                    return new RouteResult(false, Array.Empty<int>(), 0, 0);

                return new RouteResult(true, new[] { startLocationId }, 0, 0);
            }

            isBlocked ??= (_ => false); // Default: nothing is blocked

            Location? goalLocation = _world.GetLocationById(goalLocationId);
            if (goalLocation == null || isBlocked(goalLocationId) || isBlocked(startLocationId)) {
                return new RouteResult(false, Array.Empty<int>(), int.MaxValue, 0);
            }

            MinPriorityQueue<int> pq = new MinPriorityQueue<int>();
            Dictionary<int, int> gScores = new Dictionary<int, int>();
            Dictionary<int, int> previous = new Dictionary<int, int>();
            HashSet<int> visited = new HashSet<int>();

            gScores[startLocationId] = 0;

            // Priority is f(n) = g(n) + h(n)
            int startHeuristic = ComputeHeuristic(startLocationId, goalLocationId);
            pq.Enqueue(startLocationId, startHeuristic);

            int expandedCount = 0;
            bool found = false;

            while (pq.Count > 0) {
                int currentId = pq.Dequeue();

                if (!visited.Add(currentId)) {
                    continue;
                }

                expandedCount++;

                if (currentId == goalLocationId) {
                    found = true;
                    break;
                }

                int currentG = gScores[currentId];

                foreach (var connection in _world.GetOutgoing(currentId)) {
                    int neighborId = connection.ToLocationId;

                    if (isBlocked(neighborId)) {
                        continue;
                    }

                    int tentativeG = currentG + connection.TravelCost;

                    if (!gScores.TryGetValue(neighborId, out int existingG) || tentativeG < existingG) {
                        gScores[neighborId] = tentativeG;
                        previous[neighborId] = currentId;

                        int fCost = tentativeG + ComputeHeuristic(neighborId, goalLocationId);
                        pq.Enqueue(neighborId, fCost);
                    }
                }
            }

            if (!found) {
                return new RouteResult(false, Array.Empty<int>(), int.MaxValue, expandedCount);
            }

            // Reconstruct path
            List<int> path = new List<int>();
            int curr = goalLocationId;
            while (curr != startLocationId) {
                path.Add(curr);
                curr = previous[curr];
            }
            path.Add(startLocationId);
            path.Reverse(); // needs to be reversed

            return new RouteResult(true, path, gScores[goalLocationId], expandedCount);
        }

        private int ComputeHeuristic(int locationId, int goalId) {
            Location? loc = _world.GetLocationById(locationId);
            Location? goal = _world.GetLocationById(goalId);

            if (loc == null || goal == null) return 0;

            // Use absolute difference on X and Y positions as a heuristic for grids
            // X represents row
            // Y represents column
            int dx = (int)Math.Abs(loc.Position.X - goal.Position.X);
            int dy = (int)Math.Abs(loc.Position.Y - goal.Position.Y);

            return dx + dy;
        }
    }
}
