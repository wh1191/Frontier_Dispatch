using System;
using NUnit.Framework;
using FrontierDispatch.Core;

namespace FrontierDispatch.Tests
{
    /// <summary>
    /// Small contract examples for later assignment phases.
    ///
    /// These are not an exhaustive assignment test suite.
    /// Students are expected to enable the relevant tests as they reach each
    /// phase and add meaningful tests of their own, including edge cases.
    /// </summary>
    public sealed class AssignmentStarterTests
    {
        // These tests document later assignment contracts without making the
        // untouched baseline red. Remove Ignore when you reach the named phase.

        //[Test, Ignore("Phase 3: enable when implementing the binary min-heap priority queue.")]
        [Test]
        public void PriorityQueue_EnqueueAndDequeue_ReturnsLowestPriorityFirst()
        {
            var queue = new MinPriorityQueue<string>();
            queue.Enqueue("expensive", 9);
            queue.Enqueue("cheap", 2);
            queue.Enqueue("medium", 5);
            Assert.That(queue.Dequeue(), Is.EqualTo("cheap"));
        }

        //[Test, Ignore("Phase 4: enable when implementing Dijkstra route finding.")]
        [Test]
        public void Dijkstra_StartEqualsGoal_ReturnsZeroCostRoute()
        {
            var finder = new DijkstraRouteFinder(WorldDatasetFactory.CreateSmall());
            RouteResult result = finder.FindRoute(0, 0);
            Assert.That(result.Found, Is.True);
            Assert.That(result.TotalCost, Is.EqualTo(0));
            Assert.That(result.LocationIds, Is.EqualTo(new[] { 0 }));
        }

        //[Test, Ignore("Phase 5: enable when implementing bounded reachability.")]
        [Test]
        public void Reachability_ZeroBudget_IncludesStartAtZeroCost()
        {
            var query = new ReachabilityQuery(WorldDatasetFactory.CreateSmall());
            var result = query.FindReachable(0, 0);
            Assert.That(result.ContainsKey(0), Is.True);
            Assert.That(result[0], Is.EqualTo(0));
        }

        [Test]
        public void Reachability_MaxBudget_ReturnsWithinCostLimit() {
            WorldData world = WorldDatasetFactory.CreateSmall();
            ReachabilityQuery query = new ReachabilityQuery(world);
            int maxBudget = 3;

            var reachable = query.FindReachable(startLocationId: 0, maxTravelCost: maxBudget);

            Assert.That(reachable.ContainsKey(0), Is.True);
            foreach (var kvp in reachable) {
                Assert.That(kvp.Value, Is.LessThanOrEqualTo(maxBudget),
                    $"Location {kvp.Key} cost {kvp.Value} exceeds max budget {maxBudget}");
            }
        }

        [Test]
        public void AStar_WithTraversalConstraint_AvoidsBlockedLocation() {
            var world = WorldDatasetFactory.CreateSmall();
            var finder = new AStarRouteFinder(world);

            // Choose a start and goal, and block an intermediate location that Dijkstra/A* would normally use
            int start = 0;
            int goal = 5; // Adjust based on your small world layout, or use standard nodes

            // Let's block a specific location ID along the direct path
            int blockedNode = 2;

            RouteResult result = finder.FindRoute(start, goal, isBlocked: id => id == blockedNode);

            // Assert that either no route is found (if completely trapped) or the path successfully bypasses the blocked node
            if (result.Found) {
                Assert.That(result.LocationIds, Does.Not.Contain(blockedNode), 
                    "The A* route should not traverse the blocked constraint.");
            } else {
                Assert.Pass("Correctly identified that the goal is unreachable when the constraint blocks the path.");
            }
        }

        [Test]
        public void AStar_WithWallConstraint_ReroutesSuccessfully() {
            var world = WorldDatasetFactory.CreateMedium(); // 50 x 50 = 2500
            var aStar = new AStarRouteFinder(world);

            int start = 0;
            int goal = 2499;

            // Create a vertical "wall" blocking a strip of nodes in column 10
            // (blocking nodes from y = 0 to y = 40 in column 10)
            int gridWidth = 50;
            bool IsWall(int id) {
                int x = id % gridWidth;
                int y = id / gridWidth;
                return x == 10 && y < 40; // Wall blocking most of column 10
            }

            var resultAStar = aStar.FindRoute(start, goal, isBlocked: IsWall);

            Assert.That(resultAStar.Found, Is.True, "A* should find a route around the wall.");
            foreach (int node in resultAStar.LocationIds) {
                Assert.That(IsWall(node), Is.False, $"Route should not contain blocked node {node}.");
            }

            TestContext.Progress.WriteLine($"A* with Wall -> Found: {resultAStar.Found}, Cost: {resultAStar.TotalCost}, " +
                $"Nodes Expanded: {resultAStar.ExpandedCount}");
        }

        [Test]
        public void Compare_DijkstraVsAStar_GeneratesReportMetrics() {
            WorldData world = WorldDatasetFactory.CreateSmall();

            // Pick a representative start and goal across the large world
            int start = 0;
            int goal = world.Locations.Count - 1; // Last node in the dataset

            RunComparisonScenario(world, "Small (5x5=25)", start, goal);
        }

        [Test]
        public void Compare_DijkstraVsAStar_MultiScenarioBenchmarks() {
            RunComparisonScenario(WorldDatasetFactory.CreateMedium(), "Medium (50x50=2500, Corner-to-Corner)", start: 0, goal: 2499);
            RunComparisonScenario(WorldDatasetFactory.CreateMedium(), "Medium (50x50=2500, Mid-Target)", start: 0, goal: 1225);
            RunComparisonScenario(WorldDatasetFactory.CreateLarge(), "Large (200x200=40 000, Corner-to-Corner)", start: 0, goal: 39999);
            RunComparisonScenario(WorldDatasetFactory.CreateLarge(), "Large (200x200=40 000, A* Pruning)", start: 10000, goal: 26967);
        }

        private void RunComparisonScenario(WorldData world, string scenarioName, int start, int goal) {
            DijkstraRouteFinder dijkstra = new DijkstraRouteFinder(world);
            AStarRouteFinder aStar = new AStarRouteFinder(world);

            // Warmup JIT for more accurate test later
            dijkstra.FindRoute(start, start);
            aStar.FindRoute(start, start);

            // --- Dijkstra ---
            var swDijkstra = System.Diagnostics.Stopwatch.StartNew();
            var resDijkstra = dijkstra.FindRoute(start, goal);
            swDijkstra.Stop();

            // --- A* ---
            var swAStar = System.Diagnostics.Stopwatch.StartNew();
            var resAStar = aStar.FindRoute(start, goal);
            swAStar.Stop();

            PrintComparisonResult(scenarioName, resDijkstra, swDijkstra.Elapsed.TotalMilliseconds, resAStar, swAStar.Elapsed.TotalMilliseconds);
        }

        // Output data for the report table
        private static void PrintComparisonResult(string scenarioName, RouteResult dijkstraResult, double dijkstraMs, 
            RouteResult aStarResult, double aStarMs) 
        {
            TestContext.Progress.WriteLine("==========================================================================");
            TestContext.Progress.WriteLine("                    DIJKSTRA VS A* COMPARISON                             ");
            TestContext.Progress.WriteLine("==========================================================================");
            TestContext.Progress.WriteLine("\n");
            TestContext.Progress.WriteLine($"SCENARIO: {scenarioName}");
            TestContext.Progress.WriteLine($"Dijkstra -> Found: {dijkstraResult.Found}, Cost: {dijkstraResult.TotalCost}, " +
                $"Nodes Expanded: {dijkstraResult.ExpandedCount:N0}, Time: {dijkstraMs:F3} ms");
            TestContext.Progress.WriteLine($"A*       -> Found: {aStarResult.Found}, Cost: {aStarResult.TotalCost}, " +
                $"Nodes Expanded: {aStarResult.ExpandedCount:N0}, Time: {aStarMs:F3} ms");
            TestContext.Progress.WriteLine("\n");
            TestContext.Progress.WriteLine("==========================================================================");
            TestContext.Progress.WriteLine("\n");
        }

        [Test]
        public void PriorityQueue_Peek_ReturnsLowestPriorityWithoutRemoving() {
            MinPriorityQueue<string> queue = new MinPriorityQueue<string>();
            queue.Enqueue("medium", 5);
            queue.Enqueue("cheap", 1);
            queue.Enqueue("expensive", 10);

            Assert.That(queue.Peek(), Is.EqualTo("cheap"));
            Assert.That(queue.Count, Is.EqualTo(3), "Peek should not decrease queue count.");
        }

        [Test]
        public void PriorityQueue_EmptyOperations_ThrowsInvalidOperationException() {
            MinPriorityQueue<int> queue = new MinPriorityQueue<int>();
            Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
            Assert.Throws<InvalidOperationException>(() => queue.Peek());
        }

        [Test]
        public void Dijkstra_And_AStar_NonExistentLocations_ReturnUnsuccessfulResult() {
            WorldData world = WorldDatasetFactory.CreateSmall();
            DijkstraRouteFinder dijkstra = new DijkstraRouteFinder(world);
            AStarRouteFinder aStar = new AStarRouteFinder(world);

            int invalidId = 99999;
            var resultD = dijkstra.FindRoute(0, invalidId);
            var resultA = aStar.FindRoute(0, invalidId);

            Assert.That(resultD.Found, Is.False);
            Assert.That(resultA.Found, Is.False);
        }


    }
}
