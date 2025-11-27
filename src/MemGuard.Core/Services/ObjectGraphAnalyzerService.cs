using MemGuard.Core.Interfaces;
using Microsoft.Diagnostics.Runtime;

namespace MemGuard.Core.Services;

public class ObjectGraphAnalyzerService : IObjectGraphAnalyzer
{
    private readonly ClrHeap _heap;
    private readonly Dictionary<ulong, List<ulong>> _forwardRefs = new();
    private readonly Dictionary<ulong, List<ulong>> _backRefs = new();
    private readonly Dictionary<ulong, int> _refCounts = new();

    public ObjectGraphAnalyzerService(ClrHeap heap)
    {
        _heap = heap;

    }

    public void BuildObjectGraph(IEnumerable<ulong> rootObjects)
    {
        var visited = new HashSet<ulong>();
        var queue = new Queue<ulong>(rootObjects);

        Console.WriteLine("Building object graph...");

        while (queue.Count > 0)
        {
            var objAddr = queue.Dequeue();
            if (!visited.Add(objAddr)) continue;

            var obj = _heap.GetObject(objAddr);
            if (!obj.IsValid) continue;

            // Get all references from this object
            var refs = new List<ulong>();
            var EnumrateObject = obj.EnumerateReferences();
            if (EnumrateObject != null)
            {
                foreach (var child in EnumrateObject)
                {
                    if (child != 0)
                    {
                        refs.Add(child);

                        // Build back-reference map
                        if (!_backRefs.ContainsKey(child))
                            _backRefs[child] = new List<ulong>();
                        _backRefs[child].Add(objAddr);

                        // Count references
                        _refCounts[child] = _refCounts.GetValueOrDefault(child, 0) + 1;

                        queue.Enqueue(child);
                    }
                }
            }

            _forwardRefs[objAddr] = refs;
        }

        Console.WriteLine($"Graph built: {visited.Count} objects analyzed");
    }

    // Get all objects referencing a specific object (back references)
    public List<ulong> GetBackReferences(ulong objAddr)
    {
        return _backRefs.GetValueOrDefault(objAddr, new List<ulong>());
    }

    // Get reference count for an object
    public int GetReferenceCount(ulong objAddr)
    {
        return _refCounts.GetValueOrDefault(objAddr, 0);
    }

    // Find all paths from roots to a target object
    public List<List<ulong>> FindPathsToObject(ulong target, int maxPaths = 10)
    {
        var paths = new List<List<ulong>>();
        var currentPath = new List<ulong>();
        var visited = new HashSet<ulong>();

        Console.WriteLine($"Finding paths to object 0x{target:X}...");

        // Start from GC roots
        foreach (var root in _heap.EnumerateRoots())
        {
            if (paths.Count >= maxPaths) break;
            FindPathsDFS(root.Object, target, currentPath, visited, paths, maxPaths);
        }

        return paths;
    }

    private bool FindPathsDFS(ulong current, ulong target, List<ulong> path,
        HashSet<ulong> visited, List<List<ulong>> results, int maxPaths)
    {
        if (results.Count >= maxPaths) return true;
        if (!visited.Add(current)) return false;

        path.Add(current);

        if (current == target)
        {
            results.Add(new List<ulong>(path));
            path.RemoveAt(path.Count - 1);
            visited.Remove(current);
            return results.Count >= maxPaths;
        }

        if (_forwardRefs.TryGetValue(current, out var refs))
        {
            foreach (var child in refs)
            {
                if (FindPathsDFS(child, target, path, visited, results, maxPaths))
                    return true;
            }
        }

        path.RemoveAt(path.Count - 1);
        visited.Remove(current);
        return false;
    }

    // Detect cycles in object graph
    public List<List<ulong>> DetectCycles(int maxCycles = 5)
    {
        var cycles = new List<List<ulong>>();
        var visited = new HashSet<ulong>();
        var recStack = new HashSet<ulong>();
        var path = new List<ulong>();

        Console.WriteLine("Detecting cycles in object graph...");

        foreach (var objAddr in _forwardRefs.Keys)
        {
            if (cycles.Count >= maxCycles) break;
            if (!visited.Contains(objAddr))
            {
                DetectCycleDFS(objAddr, visited, recStack, path, cycles, maxCycles);
            }
        }

        Console.WriteLine($"Found {cycles.Count} cycles");
        return cycles;
    }

    private bool DetectCycleDFS(ulong objAddr, HashSet<ulong> visited,
        HashSet<ulong> recStack, List<ulong> path, List<List<ulong>> cycles, int maxCycles)
    {
        if (cycles.Count >= maxCycles) return true;

        visited.Add(objAddr);
        recStack.Add(objAddr);
        path.Add(objAddr);

        if (_forwardRefs.TryGetValue(objAddr, out var refs))
        {
            foreach (var child in refs)
            {
                if (!visited.Contains(child))
                {
                    if (DetectCycleDFS(child, visited, recStack, path, cycles, maxCycles))
                        return true;
                }
                else if (recStack.Contains(child))
                {
                    // Cycle detected
                    var cycleStart = path.IndexOf(child);
                    var cycle = path.Skip(cycleStart).ToList();
                    cycles.Add(cycle);

                    if (cycles.Count >= maxCycles)
                        return true;
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        recStack.Remove(objAddr);
        return false;
    }

    // Calculate retained size using dominator tree concept
    public Dictionary<ulong, long> CalculateRetainedSizes()
    {
        Console.WriteLine("Calculating retained sizes (simplified dominator approach)...");

        var retainedSizes = new Dictionary<ulong, long>();
        var processed = new HashSet<ulong>();

        // For each object, calculate what would be freed if it was collected
        foreach (var objAddr in _forwardRefs.Keys.Take(1000)) // Limit for performance
        {
            retainedSizes[objAddr] = CalculateRetainedSize(objAddr, processed);
        }

        return retainedSizes.OrderByDescending(x => x.Value)
            .Take(20)
            .ToDictionary(x => x.Key, x => x.Value);
    }

    private long CalculateRetainedSize(ulong objAddr, HashSet<ulong> globalProcessed)
    {
        var reachableWithout = new HashSet<ulong>();
        var reachableWith = new HashSet<ulong>();

        // Simulate: what's reachable if we remove this object?
        foreach (var root in _heap.EnumerateRoots().Take(100))
        {
            if (root.Object == objAddr) continue;
            TraverseFrom(root.Object, reachableWithout, objAddr);
        }

        // What's reachable normally?
        TraverseFrom(objAddr, reachableWith, 0);

        // Retained = reachable from object but not from other roots
        long retainedSize = 0;
        foreach (var addr in reachableWith)
        {
            if (!reachableWithout.Contains(addr))
            {
                var obj = _heap.GetObject(addr);
                if (obj.IsValid)
                    retainedSize += (long)obj.Size;
            }
        }

        return retainedSize;
    }

    private void TraverseFrom(ulong start, HashSet<ulong> reachable, ulong exclude)
    {
        var queue = new Queue<ulong>();
        queue.Enqueue(start);

        while (queue.Count > 0 && reachable.Count < 10000) // Limit for performance
        {
            var current = queue.Dequeue();
            if (current == exclude || !reachable.Add(current)) continue;

            if (_forwardRefs.TryGetValue(current, out var refs))
            {
                foreach (var child in refs)
                {
                    if (child != exclude && child != 0)
                        queue.Enqueue(child);
                }
            }
        }
    }

    // Print detailed object information with references
    public void PrintObjectDetails(ulong objAddr)
    {
        var obj = _heap.GetObject(objAddr);
        if (!obj.IsValid) return;

        Console.WriteLine($"\n=== Object Analysis: 0x{objAddr:X} ===");
        Console.WriteLine($"Type: {obj.Type?.Name ?? "Unknown"}");
        Console.WriteLine($"Size: {obj.Size} bytes");
        // Attempt to call GetGeneration if available; fall back to "N/A" for older runtimes
        var generation = -1;
        var getGenMethod = _heap.GetType().GetMethod("GetGeneration", new Type[] { typeof(ulong) });
        if (getGenMethod != null)
        {
            var genVal = getGenMethod.Invoke(_heap, new object[] { objAddr });
            if (genVal is int g) generation = g;
        }
        Console.WriteLine($"Generation: {(generation >= 0 ? generation.ToString() : "N/A")}");
        Console.WriteLine($"Reference Count: {GetReferenceCount(objAddr)}");

        // Forward references
        Console.WriteLine("\nReferences TO other objects:");
        if (_forwardRefs.TryGetValue(objAddr, out var forwards))
        {
            foreach (var refAddr in forwards.Take(10))
            {
                var refObj = _heap.GetObject(refAddr);
                Console.WriteLine($"  -> 0x{refAddr:X} ({refObj.Type?.Name}) [{refObj.Size} bytes]");
            }
            if (forwards.Count > 10)
                Console.WriteLine($"  ... and {forwards.Count - 10} more");
        }

        // Back references
        Console.WriteLine("\nReferences FROM other objects:");
        var backs = GetBackReferences(objAddr);
        foreach (var refAddr in backs.Take(10))
        {
            var refObj = _heap.GetObject(refAddr);
            Console.WriteLine($"  <- 0x{refAddr:X} ({refObj.Type?.Name}) [{refObj.Size} bytes]");
        }
        if (backs.Count > 10)
            Console.WriteLine($"  ... and {backs.Count - 10} more");
    }



    public List<ulong> GetBackReferences(ulong objectAddress, int maxDepth)
    {
        throw new NotImplementedException();
    }

    public int GetObjectReferenceCount(ulong objectAddress)
    {
        return _refCounts.GetValueOrDefault(objectAddress, 0);
    }



}
