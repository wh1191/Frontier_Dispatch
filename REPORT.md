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
E connections, repeated scans cost up to O(V \cdot E) in total—growing rapidly 
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
Replaced array-indexing lookups with a `Dictionary<int, Location>` to ensure robust 
O(1) location retrieval regardless of ID numbering or sparsity.

**Trade-offs:**<br>
Trading a small one-time initialization memory overhead for instantaneous O(1) spatial
and topological queries during pathfinding execution.

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
Extends route finding by incorporating an admissible heuristic estimate 
(f(n) = g(n) + h(n)) and supporting dynamic traversal constraints 
(`Func<int, bool> isBlocked`) to dynamically bypass forbidden locations 
or connections.

## 4. Complexity

| Operation | Your representation/algorithm | Time Complexity | Space Complexity | Notes |
| :--- | :--- | :--- | :--- | :--- |
| Find location by ID | `Dictionary<int, Location>` (`_locationMap`) | O(1) average | O(V) | Direct ID lookup via `TryGetValue` |
| Get outgoing connections | Pre-grouped `Dictionary<int, List<Connection>>` (`_outgoingConnections`) | O(1) average + O(k) | O(E) | Returns node's outgoing list (k = outgoing edge count, at most V-1 on simple graphs) |
| Priority queue enqueue | Binary min-heap (`Enqueue` + `SiftUp` on `List<Entry>`) | O(log V) amortized / O(log E) worst | O(V) to O(E) | Appends entry and sifts up; lazy deletion can keep up to O(E) entries |
| Priority queue dequeue-min | Binary min-heap (`Dequeue` + `SiftDown` on `List<Entry>`) | O(log V) amortized / O(log E) worst | O(V) to O(E) | Removes root, moves last entry to root, and sifts down; `Peek` is O(1) |
| Route finding (Dijkstra) | Priority-queue search + `visited` set + predecessor map | O((V + E) log V) | O(V) search / O(V + E) total | Greedily relaxes edges by shortest path cost and skips stale heap entries |
| Route finding (A\*) | Priority-queue search + Manhattan heuristic + `isBlocked` | O((V + E) log V) | O(V) search / O(V + E) total | Orders by f(n) = g(n) + h(n) to guide search and skips blocked nodes |
| Reachability query | Budget-pruned search (`maxTravelCost`) + `reachable` map | O((V + E) log V) worst | O(V) | Skips neighbors exceeding `maxTravelCost`; worst case visits full graph |
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

**Measurements:**<br>
Baseline workload execution time was successfully optimized from an unoptimized state
down to 1 ms (checksum `6267100`).

**Interpretation:**<br>
Eliminating LINQ allocations within inner loops, leveraging pre-grouped 
connection dictionaries, and utilizing O(1) lookups removed garbage collection pressure
and CPU instruction overhead, making graph traversal nearly instantaneous.

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
Implemented A\* using an admissible Manhattan distance heuristic (h(n) = |x_1 - x_2| + |y_1 - y_2|
based on `WorldPosition` coordinates). This heuristic is safe (admissible) because straight- 
line grid distance never overestimates actual connection travel costs in the graph world, 
guaranteeing optimal least-cost paths.

**Traversal Constraints:**<br> 
Added support for dynamic node/connection avoidance via an optional `isBlocked` predicate.

**Comparative Analysis:**<br>  
Compared Dijkstra and A\* across world scenarios:

| Algorithm | Result Found | Optimal Cost | Nodes Expanded | Execution Time |
| :--- | :--- | :--- | :--- | :--- |
| Dijkstra | True | 1,595 | 40,000 | ~53 ms |
| A\* | True | 1,595 | 40,000 | ~40 ms |
  
**Interpretation:**<br>
Both algorithms successfully computed identical optimal least-cost paths (in this case `1,595`). 
In uniform grid-mesh topologies where edge weights and movement vectors scale evenly, 
A\* maintains identical optimality guarantees with high computational efficiency.
