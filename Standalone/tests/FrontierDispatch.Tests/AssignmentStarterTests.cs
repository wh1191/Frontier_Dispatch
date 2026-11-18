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
        public void Compare_DijkstraVsAStar_GeneratesReportMetrics() {
            var world = WorldDatasetFactory.CreateLarge();
            var dijkstra = new DijkstraRouteFinder(world);
            var aStar = new AStarRouteFinder(world);

            // Pick a representative start and goal across the large world
            int start = 0;
            int goal = world.Locations.Count - 1; // Last node in the dataset

            // --- Dijkstra ---
            var swD = System.Diagnostics.Stopwatch.StartNew();
            var resultDijkstra = dijkstra.FindRoute(start, goal);
            swD.Stop();

            // --- A* ---
            var swA = System.Diagnostics.Stopwatch.StartNew();
            var resultAStar = aStar.FindRoute(start, goal);
            swA.Stop();

            // Output data for the report table
            TestContext.Progress.WriteLine("=== DIJKSTRA VS A* COMPARISON ===");
            TestContext.Progress.WriteLine($"Dijkstra -> Found: {resultDijkstra.Found}, Cost: {resultDijkstra.TotalCost}, " +
                $"Nodes Expanded: {resultDijkstra.ExpandedCount}, Time: {swD.ElapsedMilliseconds} ms");
            TestContext.Progress.WriteLine($"A*       -> Found: {resultAStar.Found}, Cost: {resultAStar.TotalCost}, " +
                $"Nodes Expanded: {resultAStar.ExpandedCount}, Time: {swA.ElapsedMilliseconds} ms");

            // Assert that both found the exact same optimal cost
            Assert.That(resultDijkstra.TotalCost, Is.EqualTo(resultAStar.TotalCost), 
                "Both algorithms must find routes with identical optimal costs.");

            // A* should expand fewer or equal nodes compared to Dijkstra
            Assert.That(resultAStar.ExpandedCount, Is.LessThanOrEqualTo(resultDijkstra.ExpandedCount), 
                "A* should be goal-directed and expand fewer nodes than Dijkstra.");
        }
    }
}
