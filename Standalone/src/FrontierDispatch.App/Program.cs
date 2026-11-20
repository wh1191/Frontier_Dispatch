using System;
using System.Diagnostics;
using FrontierDispatch.Core;

if (args.Length > 0) {
    if (args[0] == "--baseline") {
        RunBaseline();
        return;
    } 
    
    if (args[0] == "--extended") {
        RunExtended();
        return;
    }
}

// Small sanity-check application.
// This is separate from the larger performance workload below.
WorldData world = SampleWorldFactory.CreateSmall();
var queries = new WorldQueries(world);

Console.WriteLine("Frontier Dispatch - assignment starter");
Console.WriteLine($"Small demo locations: {world.Locations.Count}");
Console.WriteLine($"Find 734: {queries.FindLocationById(734)?.Name ?? "missing"}");
Console.WriteLine();
Console.WriteLine("Run with --baseline to record the supplied baseline workload.");

static void RunBaseline()
{
    // Build the dataset before timing the repeated runtime queries.
    // This baseline therefore measures query work, not world construction.
    WorldData world = WorldDatasetFactory.CreateMedium();
    var queries = new WorldQueries(world);

    // The checksum provides a stable sanity check that the workload still
    // produces equivalent results after implementation changes.
    int checksum = 0;

    const int iterations = 5000;

    var sw = Stopwatch.StartNew();

    for (int i = 0; i < iterations; ++i)
    {
        // Deterministically visit many different location IDs rather than
        // repeatedly querying the same location.
        int id = (i * 997) % world.Locations.Count;

        Location? location = queries.FindLocationById(id);
        checksum += location?.Id ?? 0;
        checksum += queries.FindOutgoingConnections(id).Count;
    }

    sw.Stop();

    Console.WriteLine("Baseline world-query workload");
    Console.WriteLine($"Locations: {world.Locations.Count}");
    Console.WriteLine($"Connections: {world.Connections.Count}");
    Console.WriteLine($"Iterations: {iterations}");
    Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
    Console.WriteLine($"Checksum: {checksum}");
    Console.WriteLine("Absolute time varies by machine. Keep this result for before/after comparison.");
}

static void RunExtended()
{
    Console.WriteLine("==========================================================================");
    Console.WriteLine("               FRONTIER DISPATCH EXTENDED BENCHMARK                       ");
    Console.WriteLine("==========================================================================");
    Console.WriteLine();

    BenchmarkDataset("Small Grid", 5, 5, iterations: 5000);
    BenchmarkDataset("Medium Grid", 50, 50, iterations: 5000);
    BenchmarkDataset("Large Grid", 200, 200, iterations: 5000);

    /*BenchmarkPreset("Small Dataset", WorldDatasetFactory.CreateSmall(), iterations: 5000);
    BenchmarkPreset("Medium Dataset", WorldDatasetFactory.CreateMedium(), iterations: 5000);
    BenchmarkPreset("Large Dataset", WorldDatasetFactory.CreateLarge(), iterations: 5000);*/
}

#pragma warning disable CS8321
static void BenchmarkDataset(string name, int width, int height, int iterations) {
    // Force GC to get clean allocation measurements for dataset generation
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    // Measure Dataset Creation & Allocation Overhead
    long initialAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
    var creationSw = Stopwatch.StartNew();

    WorldData world = WorldDatasetFactory.CreateGrid(width, height);

    creationSw.Stop();
    long creationAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - initialAllocatedBytes;

    // Measure Query Workload
    WorldQueries queries = new WorldQueries(world);
    int checksum = 0;

    // Force GC again to isolate query-phase memory allocation
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    long queryInitialBytes = GC.GetAllocatedBytesForCurrentThread();
    var querySw = Stopwatch.StartNew();

    for (int i = 0; i < iterations; ++i) {
        int id = (i * 997) % world.Locations.Count;
        Location? location = queries.FindLocationById(id);
        checksum += location?.Id ?? 0;
        checksum += queries.FindOutgoingConnections(id).Count;
    }

    querySw.Stop();
    long queryAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - queryInitialBytes;

    Console.WriteLine($"--- {name} Benchmark ---");
    Console.WriteLine($"Grid Dimensions:       {width} x {height}");
    Console.WriteLine($"Locations (V):         {world.Locations.Count:N0}");
    Console.WriteLine($"Connections (E):       {world.Connections.Count:N0}");
    Console.WriteLine($"Creation Time:         {creationSw.Elapsed.TotalMilliseconds:F3} ms");
    Console.WriteLine($"Allocation Overhead:   {creationAllocatedBytes / 1024.0:F2} KB ({creationAllocatedBytes:N0} bytes)");
    Console.WriteLine($"Iterations:            {iterations:N0}");
    Console.WriteLine($"Elapsed Time:          {querySw.Elapsed.TotalMilliseconds:F3} ms");
    Console.WriteLine($"Query Memory Overhead: {queryAllocatedBytes / 1024.0:F2} KB");
    Console.WriteLine($"Checksum:              {checksum}");
    Console.WriteLine();
}

static void BenchmarkPreset(string name, WorldData world, int iterations) {
    // Derive dimensions dynamically since presets create square grids
    // (25 -> 5x5, 2500 -> 50x50, 40 000 -> 200x200)
    int dimension = (int)Math.Sqrt(world.Locations.Count);

    // Force GC to isolate memory measurements
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    // Since the dataset is already created when passed into this method, 
    // measure only the query workload performance and query-phase memory.
    WorldQueries queries = new WorldQueries(world);
    int checksum = 0;

    long queryInitialBytes = GC.GetAllocatedBytesForCurrentThread();
    var querySw = Stopwatch.StartNew();

    for (int i = 0; i < iterations; ++i) {
        int id = (i * 997) % world.Locations.Count;
        Location? location = queries.FindLocationById(id);
        checksum += location?.Id ?? 0;
        checksum += queries.FindOutgoingConnections(id).Count;
    }

    querySw.Stop();
    long queryAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - queryInitialBytes;

    Console.WriteLine($"--- {name} ---");
    Console.WriteLine($"Grid Dimensions:       {dimension} x {dimension}");
    Console.WriteLine($"Locations (V):         {world.Locations.Count:N0}");
    Console.WriteLine($"Connections (E):       {world.Connections.Count:N0}");
    Console.WriteLine($"Iterations:            {iterations:N0}");
    Console.WriteLine($"Elapsed:               {querySw.Elapsed.TotalMilliseconds:F3} ms");
    Console.WriteLine($"Query Memory Overhead: {queryAllocatedBytes / 1024.0:F2} KB");
    Console.WriteLine($"Checksum:              {checksum}");
    Console.WriteLine();
}
#pragma warning restore CS8321
