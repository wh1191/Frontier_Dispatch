# REPORT.md - Individual Assignment

<!-- Keep this concise and specific to your implementation. -->

## 1. Initial analysis

**- How are locations and connections represented in the baseline?**<br>
Locations (`Location`) are represented as objects containing the following:<br>
`Id` (integer, all should be unique)<br>
`Name` (string)<br>
`Position` (`WorldPosition` struct with X and Y, both are float)<br>
`Type` (string)<br>

Connections (`Connection`) are directed edges defined by following:<br>
`FromLocationId` (integer, connected to the unique `Id` of a `Location`)<br>
`ToLocationId` (integer, connected to the unique `Id` of a `Location`)<br>
`TravelCost` (integer)<br>

Both Locations and Connections are initially stored in raw arrays (`Location[]` and 
`Connection[]`), which are then exposed publicly via `IReadOnlyList<Location>` and 
`IReadOnlyList<Connection>` interfaces. This does not initially provide any sort 
of built-in spatial or adjacency indexing.

**- Which operations appear frequent?**<br>
The most frequent operations happen during pathfinding and reachability queries whenever
these are retrieving location details by their ID (`GetLocationById(int id)`) and querying all 
outgoing connections for a given node (`GetOutgoing(int locationId)`) during edge relaxation loops.

**- Which operation(s) do you predict may scale poorly?**<br>
Retrieving outgoing connections via a linear scan over all connections, such as
`connections.Where(c => c.FromLocationId == id)` (where `c` is the current
`Connection` being checked) scales with the number of edges, O(E), per lookup. 
Because pathfinding can expand up to V nodes and each expansion scans up to 
E connections, repeated scans cost up to O(V \* E) in total—growing rapidly 
on larger or denser graphs.

**- What will you investigate first?**<br>
What needs to be investigated is if there's a possibility to optimize topological 
lookups by pre-grouping connections into an adjacency structure (for example using 
a Dictionary for mapping location IDs to Lists of outgoing connections). That would 
ensure location ID lookups with a O(1) time complexity, and thereby eliminating linear 
scan bottlenecks before implementing the pathfinding algorithms.


## 2. Data representation

<!-- Describe important changes/retained choices, operations affected, and time/memory trade-offs. -->

**Retained Structures:**<br> 
`Location`, `Connection`, and `WorldPosition` classes/structs representing the immutable
graph topology and spatial properties.

**Changes & Additions:**<br> 
Introduced a pre-grouped `Dictionary<int, List<Connection>>` in `WorldData` to map 
location IDs directly to their outgoing edges, eliminating runtime edge filtering.
Replaced array-indexing lookups with a `Dictionary<int, Location>` to ensure an 
improved O(1) location retrieval regardless of ID numbering or sparsity.

The benchmark `RunExtended()` was also added in `Program.cs` script for displaying extended 
amount of data for the performance analysis, also making use of necessary functions like 
`GC.GetAllocatedBytesForCurrentThread()`, `GC.Collect()` and `GC.WaitForPendingFinalizers()`.
The script also takes a new argument (`--extended`) to call the `RunExtended()` function. 

**Loading, Preparation & Dataset Scaling Costs:**
Optimized code trades a small one-time initialization cost (building dictionaries and 
adjacency lists) for instantaneous O(1) spatial and topological queries during 
pathfinding execution.

## 3. Algorithms and adaptation

<!-- Explain your route finder at a useful high level and how the travel-budget query adapts the same ideas. -->

**Core Route Finder (Dijkstra):**<br>
Uses a min-priority queue (`MinPriorityQueue<int>`) storing nodes ordered by accumulated cost 
(g-score). Explores outgoing edges greedily, relaxing costs and maintaining predecessor 
pointers for path reconstruction.

**Reachability Adaptation:**<br>
Adapted the pathfinding graph traversal into a budgeted exploration query (`maxTravelCost`). 
Nodes exceeding the remaining budget are pruned early, returning all reachable node 
IDs within the specified travel range.

**VG Extension (A\* Search):**<br> 
Extends route finding by ranking nodes using `f(n) = g(n) + h(n)`, combining accumulated
path cost `g(n)` with an admissible heuristic estimate `h(n)` toward the goal, while
accepting a `Func<int, bool> isBlocked` predicate to dynamically skip blocked locations
during search.


## 4. Complexity
*Note: Time and space complexity describe runtime and memory growth in terms of vertices (V), edges (E), and a node's outgoing edge count (k, at most V - 1 on simple graphs) across persistent graph structures and per-search state.*<br>
*Average reflects typical hash/lookup behavior, Amortized reflects per-operation cost averaged over a sequence (for example if a list resizes), and Worst reflects upper-bound/degenerate cases.*<br>
*Lazy deletion (see both Route findings (Dijkstra and A\*) and Reachability query) means pushing a new entry on a cheaper path and ignoring stale/outdated entries when popped instead of searching and removing them upfront. A heap can contain up to O(E) entries (E <= V \* (V - 1) on simple graphs).*
*Total space is O(V + E) because search structures (up to O(V) for per-node state plus up to O(E) for lazy heap entries) and persistent graph data (O(V) locations and O(E) connections) combine linearly into O(V + E).*

| Operation | Your representation/algorithm | Time Complexity | Space Complexity | Notes |
| :--- | :--- | :--- | :--- | :--- |
| Find location by ID | `Dictionary<int, Location>` (`_locationMap`) | O(1) average | O(V) | Direct ID lookup via `TryGetValue` |
| Get outgoing connections | Pre-grouped `Dictionary<int, List<Connection>>` (`_outgoingConnections`) | O(1) average + O(k) | O(E) | Returns node's outgoing list |
| Priority queue Enqueue | Binary min-heap (`Enqueue` + `SiftUp` on `List<Entry>`) | O(log V) amortized / O(log E) worst | O(V) to O(E) | Appends entry and sifts up; lazy deletion can keep up to O(E) entries |
| Priority queue Dequeue-min | Binary min-heap (`Dequeue` + `SiftDown` on `List<Entry>`) | O(log V) amortized / O(log E) worst | O(V) to O(E) | Removes root, moves last entry to root, and sifts down |
| Priority queue Peek | Binary min-heap (`Peek` on `List<Entry>`) | O(1) | O(1) | Returns root element (`_heap[0]`) without removing it |
| Route finding (Dijkstra) | Priority-queue search + `visited` set + predecessor map | O((V + E) log V) | O(V + E) search & total | Greedily relaxes edges by shortest path cost; has lazy deletion |
| Route finding (A\*) | Priority-queue search + Manhattan heuristic + `isBlocked` | O((V + E) log V) | O(V + E) search & total | Orders by f(n) = g(n) + h(n) to guide search and skips blocked nodes; has lazy deletion |
| Reachability query | Budget-pruned search (`maxTravelCost`) + `reachable` map | O((V + E) log V) worst | O(V + E) search & total | Skips neighbors exceeding `maxTravelCost`; worst case visits full graph; has lazy deletion |
| Graph initialization | `WorldData` constructor (`ToDictionary` + loop over connections) | O(V + E) | O(V + E) | Copies inputs to arrays and builds ID and outgoing-edge dictionaries once |

## 5. Correctness and testing

<!-- Important tests/edge cases and what they demonstrate. -->

**Start Equals Goal:**<br>
Verified that querying a route where `start == goal` instantly returns a valid path 
of length 1 with zero cost.

**Unreachable Goals & Blocked Constraints:**<br> 
Tested traversal constraints by supplying an `isBlocked` predicate, ensuring A\* 
successfully routes around blocked nodes or correctly flags paths as unreachable.

**Priority Queue & Optimality:**<br> 
Validated that the custom priority queue extracts minimum-cost elements correctly, 
ensuring least-cost paths are found even when they require traversing more edges 
than direct alternatives.

## 6. Performance investigation

<!-- Record reproducible before/after measurements for loading/preparation, repeated runtime work, and relevant allocation behavior. Explain what changed and why. -->

**Measurement Methodology:**<br>
Performance and memory allocation data were gathered using an extended benchmark harness 
(--extended flag in Program.cs):

- Execution Timing: Measured using high-resolution `System.Diagnostics.Stopwatch` timestamps
around 5,000 query iterations.

- Memory Tracking: Managed memory footprint and allocation rates were captured using 
`System.GC.GetAllocatedBytesForCurrentThread()` immediately before and after query workload 
execution (with garbage collection forced beforehand to isolate results).

- Workload Scope: Executed 5,000 deterministic iterations across varying location IDs 
(`int id = (i \* 997) % world.Locations.Count;`).

**Comprehensive Benchmark Results (Old Code, Unoptimized, Linear Scan):**<br>

| Dataset Size | Grid Dimensions | Locations (V) | Connections (E) | Creation Time | Allocation Overhead | Iterations | Elapsed Time | Query Memory Overhead | Checksum |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Small | 5 x 5 | 25 | 80 | 2.394 ms | 14.84 KB | 5,000 | 4.137 ms | 429.73 KB | 76,000 |
| Medium | 50 x 50 | 2,500 | 9,800 | 0.710 ms | 1,569.61 KB | 5,000 | 451.732 ms | 429.73 KB | 6,267,100 |
| Large | 200 x 200 | 40,000 | 159,200 | 42.807 ms | 25,799,20 KB | 5,000 | 6,809.801 ms | 429.73 KB | 99,787,400 |

**Comprehensive Benchmark Results (New Code, Optimized, Indexed / Dictionary):**<br>

| Dataset Size | Grid Dimensions | Locations (V) | Connections (E) | Creation Time | Allocation Overhead | Iterations | Elapsed Time | Query Memory Overhead | Checksum |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Small | 5 x 5 | 25 | 80 | 2.432 ms | 14.84 KB | 5,000 | 0.304 ms | 0.04 KB | 76,000 |
| Medium | 50 x 50 | 2,500 | 9,800 | 1.075 ms | 1,569.61 KB | 5,000 | 0.116 ms | 0.04 KB | 6,267,100 |
| Large | 200 x 200 | 40,000 | 159,200 | 28.343 ms | 25,799.20 KB | 5,000 | 0.736 ms | 0.04 KB | 99,787,400 |

**Side-by-Side Comparison (Medium Dataset Size):**<br>

| Metric | Unoptimized (Linear Scan) | Optimized (Indexed / Dictionary) | Performance Delta |
| :--- | :--- | :--- | :--- |
| Grid Dimensions | 50 x 50 | 50 x 50 | Identical topology |
| Locations (V) / Connections (E) | 2,500 / 9,800 | 2,500 / 9,800 | Identical dataset |
| Creation Time | ~0.710 ms | ~1.075 ms | Negliglible difference due to efficient O(V + E) linear pass and fast .NET heap memory allocation |
| Dataset Allocation Overhead | 1,569.61 KB | 1,569.61 KB | Identical, but there could potentially be minor dictionary indexing overhead in the opimized code |
| Iterations | 5,000 | 5,000 | Identical workload |
| Elapsed Time | ~451.732 ms | ~0.116 ms | Optimized is ~3,894.2 times faster |
| Query Memory Overhead | 429.73 KB | 0.04 KB | Eliminated query-time heap garbage |
| Checksum | 6,267,100 | 6,267,100 | Verified Correctness Match |

**Analysis of Improvements & Trade-offs:**<br>

**- Massive improvemnt on Lookups with O(1):**<br>
In the unoptimized version, every outgoing connection query performed a linear scan over 
the edge list (O(E)), causing elapsed time to increase significantly on Medium and Large 
datasets (there's definitely a difference in Small datasets as well, but it's not as 
noticeable as with other datasets). Pre-grouping connections into adjacency dictionaries 
reduced query times to sub-millisecond levels across all dataset sizes.

**- Garbage Collection Relief:**<br>
The unoptimized code's query phase generated recurring transient heap allocations by LINQ
queries (~429 KB) due to dynamic filtering/list instantiations inside inner loops. 
The optimized implementation dropped query memory overhead to a negligible 0.04 KB, 
protecting the application from GC stalls (overhead, extra CPU instructions etc), making 
graph traversal nearly instantaneous.

**- The Upfront Cost Trade-off:**<br>
Indexing introduces a one-time setup cost during dataset creation (scaling from ~14.8 KB for 
Small up to ~25.8 MB for Large). However, this upfront allocation pays for itself instantly 
during repeated pathfinding and reachability queries.

**- Correctness Validation:**<br>
Across all benchmark runs, the persistent Checksum remained perfectly stable, confirming that 
the optimized lookup logic produces entirely equivalent, correct results.


## 7. Development reflection

<!-- Describe one bug, wrong assumption, failed approach, or surprising result and how you investigated it. -->

**The Bug:**<br>
During early testing, the starter test `FindLocationById_ReturnsMatchingLocation` threw a
`NullReferenceException`.

**Investigation:**<br>
The initial implementation of `GetLocationById` assumed a direct array index look-up 
(`_locations[id]`), presuming location IDs were strictly contiguous integers starting 
at `0`. However, the dataset's location IDs did not align with 0-based array indices.

**Resolution:**<br>
Replaced array indexing with a `Dictionary<int, Location>` map built during
initialization. This decoupled internal storage order from location ID identifiers while
preserving O(1) performance, immediately resolving the test failure.

## 8. Tools and AI assistance

<!-- Briefly describe relevant assistance, if any. -->

Some AI assistance was used during debugging and to also check whether the 
first implemented solutions made any logical sense or even functional during the 
implementation process. These include debugging nullable reference annotations, 
architectural structuring, and refining heuristic admissibility logic.


## 9. VG extension

<!-- Only if attempting VG: heuristic, traversal constraints, correctness, and Dijkstra/A\* comparison. -->

**Heuristic & Safety:**<br>
Implemented A\* with a Manhattan distance heuristic (`h(n) = |x_1 - x_2| + |y_1 - y_2|`)
computed from `WorldPosition` coordinates to the goal. This specific heuristic was used 
because the WorldData is presented as a 2D grid with 4-directional cardinal movement 
(up, down, left, right).<br> 
It is admissible when the condition `h(n) <= h*(n)` holds for every node `n`, where 
`h(n)` is the estimated remaining cost from `n` to the goal and `h*(n)` is the true 
minimum remaining cost to the goal. Because A\* never overestimates what remains, 
it will not hide or bypass a cheaper path behind an inflated estimate, preserving 
optimal least-cost paths.<br>
Reaching the goal requires at least `|x_1 - x_2| + |y_1 - y_2|` grid steps, so as 
long as edge `TravelCost` is at least 1 per coordinate unit, Manhattan distance 
remains admissible (whereas sub-unit values less than 1, such as fractional or 
non-positive `TravelCost` values, could violate admissibility).

**Traversal Constraints:**<br>
Added an optional `Func<int, bool> isBlocked` predicate to dynamically skip blocked
locations during search, allowing A\* to route around obstacles or return no path
when no valid route remains.

**Memory & Allocations:**<br>
Both algorithms share the same core search structures (`g`-score/priority queue state,
visited tracking, and predecessor pointers), keeping peak memory comparable. For A\*, 
Computing Manhattan distance and evaluating `isBlocked` introduces negligible overhead 
without adding extra per step heap allocations.

**Comparison Results:**<br>
*Compared Dijkstra and A\* across world scenarios.*

| Scenario | Start and Goal | Algorithm | Goal Found | Optimal Cost | Nodes Expanded | Execution Time |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| Small (5x5) | From 0 to 24 | Dijkstra | True | 26 | 24 | 0.019 ms |
| Small (5x5) | From 0 to 24 | A\* | True | 26 | 24 | 0.139 ms |
| Medium (50x50, Corner-to-Corner) | From 0 to 2,499 | Dijkstra | True | 251 | 2,316 | 1.218 ms |
| Medium (50x50, Corner-to-Corner) | From 0 to 2,499 | A\* | True | 251 | 1,798 | 0.630 ms |
| Medium (50x50, Mid-Target) | From 0 to 1,225 | Dijkstra | True | 129 | 631 | 0.196 ms |
| Medium (50x50, Mid-Target) | From 0 to 1,225 | A\* | True | 129 | 498 | 0.185 ms |
| Large (200x200, Corner-to-Corner) | From 0 to 39,999 | Dijkstra | True | 1,595 | 40,000 | 20.819 ms |
| Large (200x200, Corner-to-Corner) | From 0 to 39,999 | A\* | True | 1,595 | 40,000 | 19.381 ms |
  
**Analysis & Insights:**<br>
Both algorithms successfully computed identical optimal least-cost paths (in this case `1,595`). 
In uniform grid-mesh topologies where edge weights and movement vectors scale evenly, 
A\* maintains identical optimality guarantees with high computational efficiency.
