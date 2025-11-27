# MemGuard

| Feature                   | Supported?                       | Source                              |
| ------------------------- | -------------------------------- | ----------------------------------- |
| Heap walking              | ✔                                | AnalyzeHeap()                       |
| Type stats (count + size) | ✔                                | AnalyzeHeap()                       |
| Retained size             | ✔ (heuristic + partial accurate) | ComputeRetainedSizeForObject()      |
| Retention path            | ✔                                | FindRetentionPath()                 |
| Object liveness           | ✔                                | IsObjectAlive()                     |
| GC segment map            | ✔                                | SegmentSummary()                    |
| Feature availability map  | ✔                                | _featureStatuses                    |
| LOH detection             | ✔                                | SegmentSummary.IsLargeObjectSegment |
| Dump file analysis        | ✔                                | LoadTarget(dumpPath)                |
| Live process attach       | ✔                                | LoadTarget(pid)                     |
| Allocation profiling      | ❌ (listed but not implemented)   | N/A                                 |
| GC event timeline         | ❌                                | N/A                                 |
| CPU sampling              | ❌                                | N/A                                 |
| Thread pool starvation    | ❌                                | N/A                                 |
