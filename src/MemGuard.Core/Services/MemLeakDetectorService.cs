// MemLeakDetectorService.cs
using MemGuard.Core.Interfaces;
using Microsoft.Diagnostics.Runtime;
using System.Collections.Concurrent;
using System.Text;

namespace MemGuard.Core.Services;

public sealed class MemLeakDetectorService : IMemLeakDetector, IDisposable
{
    private DataTarget? _dataTarget;
    private ClrRuntime? _runtime;
    private ClrHeap? _heap;

    private readonly ConcurrentDictionary<ulong, RetentionPath?> _retentionCache = new();

    public MemLeakDetectorService(string? dumpPath = null, int? processId = null)
    {
        if (!string.IsNullOrWhiteSpace(dumpPath))
            LoadDumpFile(dumpPath);
        else if (processId.HasValue)
            AttachToProcess(processId.Value);
    }

    public void LoadDumpFile(string dumpPath)
    {
        if (!File.Exists(dumpPath))
            throw new FileNotFoundException($"Memory dump not found: {dumpPath}");

        ClearTarget();
        _dataTarget = DataTarget.LoadDump(dumpPath);
        InitializeRuntime();
    }

    public void AttachToProcess(int processId)
    {
        ClearTarget();
        _dataTarget = DataTarget.AttachToProcess(processId, suspend: false);
        InitializeRuntime();
    }

    private void InitializeRuntime()
    {
        if (_dataTarget == null) throw new InvalidOperationException("No data target.");

        var clrVersion = _dataTarget.ClrVersions.FirstOrDefault()
            ?? throw new InvalidOperationException("No .NET runtime found in the target.");

        _runtime = clrVersion.CreateRuntime();
        _heap = _runtime.Heap;

        if (!_heap.CanWalkHeap)
            throw new InvalidOperationException("Cannot walk the managed heap.");
    }

    public LeakReport AnalyzeHeap(AnalysisOptions options, CancellationToken ct = default)
    {
        EnsureReady();

        var report = new LeakReport();
        var typeMap = new Dictionary<string, (ulong Size, int Count, List<ulong> Samples)>();

        foreach (var obj in _heap.EnumerateObjects())
        {
            ct.ThrowIfCancellationRequested();

            if (!obj.IsValid) continue;
            var type = obj.Type;
            if (type == null) continue;

            var typeName = type.Name ?? "Unknown";
            var size = obj.Size;

            if (!typeMap.TryGetValue(typeName, out var stats))
                stats = (0, 0, new List<ulong>());

            stats.Size += size;
            stats.Count++;
            if (stats.Samples.Count < options.MaxSamplesPerType)
                stats.Samples.Add(obj.Address);

            typeMap[typeName] = stats;
        }

        // Calculate total heap size from segments
        report.TotalHeapSize = (ulong)_heap.Segments.Sum(s => (long)s.Length);
        report.TotalObjects = (ulong)typeMap.Values.Sum(x => x.Count);

        report.TopTypesByRetainedSize = typeMap
            .Select(kvp => new LeakTypeSummary
            {
                TypeName = kvp.Key,
                RetainedSize = kvp.Value.Size,
                InstanceCount = kvp.Value.Count,
                ExampleObjectAddresses = kvp.Value.Samples
            })
            .OrderByDescending(x => x.RetainedSize)
            .Take(options.TopN)
            .ToList();

        return report;
    }

    public RetentionPath? FindRetentionPath(ulong objectAddress, CancellationToken ct = default)
    {
        if (_retentionCache.TryGetValue(objectAddress, out var cached))
            return cached;

        EnsureReady();

        var path = BuildRetentionPath(objectAddress, new HashSet<ulong>(), ct);
        return _retentionCache[objectAddress] = path;
    }

    private RetentionPath? BuildRetentionPath(ulong addr, HashSet<ulong> visited, CancellationToken ct, int depth = 0)
    {
        if (depth > 100 || visited.Contains(addr)) return null;
        visited.Add(addr);

        // Check if this object is a GC root
        foreach (var root in _runtime!.Heap.EnumerateRoots())
        {
            ct.ThrowIfCancellationRequested();
            if (root.Object == addr)
            {
                var path = new List<string>
                {
                    $"GC ROOT → {root.RootKind} ({root.Object:X})"
                };
                return new RetentionPath(addr, path);
            }
        }

        var obj = _heap!.GetObject(addr);
        if (!obj.IsValid) return null;

        var type = obj.Type;
        if (type == null) return null;

        // Enumerate references from this object
        foreach (var reference in obj.EnumerateReferences())
        {
            if (!reference.IsValid || visited.Contains(reference.Address)) continue;

            var parentPath = BuildRetentionPath(reference.Address, visited, ct, depth + 1);

            if (parentPath != null)
            {
                var fullPath = new List<string>(parentPath.PathFromRoot);
                fullPath.Add($"{type.Name} → field (0x{addr:X})");
                fullPath.Reverse();
                return new RetentionPath(addr, fullPath);
            }
        }

        return null;
    }

    public ObjectInfo? GetObjectInfo(ulong objectAddress)
    {
        EnsureReady();
        var obj = _heap!.GetObject(objectAddress);
        if (!obj.IsValid) return null;

        var type = obj.Type;
        if (type == null) return null;

        // Use the heap API to determine generation if available
        int generation = 2; // Default to Gen2
        try
        {
            // Some versions of ClrMD expose GetGeneration(ulong). Use reflection to call it when available
            var heapType = _heap.GetType();
            var getGenMethod = heapType.GetMethod("GetGeneration", new[] { typeof(ulong) });
            if (getGenMethod != null)
            {
                var result = getGenMethod.Invoke(_heap, new object[] { objectAddress });
                if (result is int gen && gen >= 0)
                    generation = gen;
            }
            // If the method isn't present, keep the default (Gen2) as a safe approximation.
        }
        catch
        {
            // If reflection invocation fails for any reason, fall back to Gen2
            generation = 2;
        }

        return new ObjectInfo(
            objectAddress,
            type.Name ?? "Unknown",
            obj.Size,
            generation
        );
    }

    public bool IsObjectAlive(ulong objectAddress)
    {
        EnsureReady();
        var obj = _heap!.GetObject(objectAddress);
        return obj.IsValid && obj.Type != null && !obj.IsFree;
    }

    // THE MAIN METHOD YOU WANT
    public AnalysisResult ExecuteAnalysis(AnalysisOptions options, CancellationToken ct = default)
    {
        var report = AnalyzeHeap(options, ct);
        var diagnostics = new List<DiagnosticBase>();

        var topTypes = report.TopTypesByRetainedSize.Take(15).ToList();
        double totalHeap = report.TotalHeapSize;

        string rootCause = "No significant memory issues detected.";
        string codeFix = "";
        double confidence = 0.3;

        foreach (var t in topTypes)
        {
            double percent = t.RetainedSize * 100.0 / totalHeap;
            var sample = t.ExampleObjectAddresses.FirstOrDefault();
            var path = sample != 0 ? FindRetentionPath(sample, ct) : null;

            // Event handler / closure leak
            if (t.TypeName.Contains("DisplayClass") || t.TypeName.Contains("<>c__") || t.TypeName.Contains("EventHandler"))
            {
                rootCause = $"Classic event handler / closure leak detected. {t.InstanceCount:N0} captured delegates hold objects alive ({percent:F1}% of heap).";
                confidence = 0.96;
                codeFix = GenerateClosureLeakFix();
                break;
            }

            // Timer leak
            if (t.TypeName.Contains("System.Threading.Timer") && t.InstanceCount > 30)
            {
                rootCause = $"Many undisposed System.Threading.Timer instances ({t.InstanceCount}) are keeping callbacks alive.";
                confidence = 0.93;
                codeFix = GenerateTimerFix();
                break;
            }

            // HttpClient leak
            if (t.TypeName.Contains("HttpClient") || t.TypeName.Contains("HttpMessageHandler"))
            {
                rootCause = "Multiple short-lived HttpClient instances detected — classic socket exhaustion pattern.";
                confidence = 0.91;
                codeFix = GenerateHttpClientFix();
                break;
            }

            // Static reference leak
            if (path?.PathFromRoot.Any(p => p.Contains("Static") || p.Contains("static")) == true)
            {
                rootCause = $"Objects retained via static field/reference. Type '{t.TypeName}' is held in a static container.";
                confidence = 0.98;
                codeFix = "```csharp\n// Clear the static reference\nMyClass.StaticCache = null;\n// or\nMyService.Instance = null;\n```";
                break;
            }

            // Task leak
            if (t.TypeName.Contains("System.Threading.Tasks.Task") && t.InstanceCount > 2000)
            {
                rootCause = "Thousands of completed but unobserved Tasks — likely fire-and-forget or unobserved exceptions.";
                confidence = 0.87;
                codeFix = "Avoid Task.Run(...) without awaiting or observing exceptions.";
            }
        }

        // Pinned objects check
        var pinned = _runtime!.Heap.EnumerateRoots().Count(r => r.IsPinned);
        if (pinned > 50)
            diagnostics.Add(new PinnedObjectDiagnostic(pinned, pinned > 500 ? "Extreme" : "High"));

        // Heap fragmentation (approximate) - sum free space
        long lohFree = 0;
        foreach (var segment in _heap.Segments)
        {
            // Walk the segment and find free objects
            ulong current = segment.FirstObjectAddress;
            while (current < segment.End && current != 0)
            {
                var obj = _heap.GetObject(current);
                if (!obj.IsValid) break;
                
                if (obj.IsFree && obj.Size > 1024 * 1024)
                    lohFree += (long)obj.Size;
                
                current = obj.Address + obj.Size;
                if (current < obj.Address) break; // overflow check
            }
        }
        
        if (lohFree > 100 * 1024 * 1024)
            diagnostics.Add(new HeapDiagnostic(0.65, lohFree, (long)report.TotalHeapSize));

        return new AnalysisResult(
            RootCause: rootCause,
            CodeFix: codeFix,
            ConfidenceScore: confidence,
            Diagnostics: diagnostics
        );
    }

    #region Code Fix Generators
    private static string GenerateClosureLeakFix() => """
        ```csharp
        // BAD: Captures 'this' forever
        button.Click += (s, e) => DoWork(this.largeObject);

        // GOOD: Use weak reference or unsubscribe
        private readonly WeakReference<MyViewModel> _weakVm = new WeakReference<MyViewModel>(this);

        button.Click += Handler;
        // In Dispose/Unload:
        button.Click -= Handler;

        void Handler(object? s, EventArgs e)
        {
            if (_weakVm.TryGetTarget(out var vm))
                vm.DoWork();
        }
        ```
        """;
    
    private static string GenerateTimerFix() => """
        ```csharp
        // GOOD: Disposable timer pattern
        private Timer? _timer;
        private readonly PeriodicTimer _periodic = new(TimeSpan.FromSeconds(30));

        public async Task StartAsync(CancellationToken ct)
        {
            while (await _periodic.WaitForNextTickAsync(ct))
            {
                await DoWorkAsync();
            }
        }
        ```
        """;
    
    private static string GenerateHttpClientFix() => """
        ```csharp
        // In Program.cs / Startup.cs
        builder.Services.AddHttpClient<MyService>(client =>
        {
            client.BaseAddress = new Uri("https://api.example.com/");
        });

        // In your service
        public class MyService(IHttpClientFactory factory)
        {
            private readonly HttpClient _client = factory.CreateClient(nameof(MyService));
        }
        ```
        """;
    #endregion
    
    private void EnsureReady()
    {
        if (_heap == null || !_heap.CanWalkHeap)
            throw new InvalidOperationException("Load a dump file or attach to a process first.");
    }
    
    private void ClearTarget()
    {
        // Dispose the runtime first if present (ClrRuntime is IDisposable in some ClrMD versions)
        try
        {
            _runtime?.Dispose();
        }
        catch
        {
            // swallow any disposal exceptions to avoid throwing during cleanup
        }
        _runtime = null;

        // Dispose the data target afterwards
        try
        {
            _dataTarget?.Dispose();
        }
        catch
        {
            // swallow any disposal exceptions to avoid throwing during cleanup
        }
        _dataTarget = null;

        _heap = null;
        _retentionCache.Clear();
    }
    
    public void Dispose()
    {
        ClearTarget();
        System.GC.SuppressFinalize(this);
    }
}