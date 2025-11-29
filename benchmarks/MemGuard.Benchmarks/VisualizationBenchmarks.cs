using BenchmarkDotNet.Attributes;
using MemGuard.Cli.Visualization;

namespace MemGuard.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class VisualizationBenchmarks
{
    private CallGraphRenderer _graphRenderer = null!;
    private MemoryMapVisualizer _memoryVisualizer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _graphRenderer = new CallGraphRenderer();
        _memoryVisualizer = new MemoryMapVisualizer();
    }

    [Benchmark(Description = "Render Deadlock Graph (3 threads)")]
    public void RenderDeadlockGraph()
    {
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);

        try
        {
            var deadlockInfo = new List<(int ThreadId, List<string> WaitingOn)>
            {
                (1234, new List<string> { "Lock A", "Lock B" }),
                (5678, new List<string> { "Lock C", "Lock D" }),
                (9012, new List<string> { "Lock E", "Lock F" })
            };
            
            _graphRenderer.RenderDeadlockGraph(deadlockInfo);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Benchmark(Description = "Render Call Flow (20 calls)")]
    public void RenderCallFlow()
    {
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);

        try
        {
            var callSequence = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                callSequence.Add($"Method_{i}(args)");
            }
            
            _graphRenderer.RenderCallFlow(callSequence);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Benchmark(Description = "Render Memory Overview")]
    public void RenderMemoryOverview()
    {
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        
        try
        {
            _memoryVisualizer.RenderMemoryOverview(
                totalMemory: 1024 * 1024 * 1024,
                usedMemory: 768 * 1024 * 1024,
                freeMemory: 256 * 1024 * 1024,
                fragmentation: 0.35);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Benchmark(Description = "Render Type Distribution (10 types)")]
    public void RenderTypeDistribution()
    {
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);

        try
        {
            var topTypes = new List<(string TypeName, long Size, int Count)>
            {
                ("System.String", 50 * 1024 * 1024, 100000),
                ("System.Byte[]", 30 * 1024 * 1024, 5000),
                ("System.Char[]", 20 * 1024 * 1024, 8000),
                ("MyApp.User", 15 * 1024 * 1024, 10000),
                ("MyApp.Product", 10 * 1024 * 1024, 5000),
                ("System.Collections.Generic.List`1", 8 * 1024 * 1024, 2000),
                ("System.Collections.Generic.Dictionary`2", 7 * 1024 * 1024, 1500),
                ("System.Int32[]", 5 * 1024 * 1024, 1000),
                ("System.Object[]", 4 * 1024 * 1024, 800),
                ("MyApp.Cache", 3 * 1024 * 1024, 100)
            };
            
            _memoryVisualizer.RenderTypeDistribution(topTypes);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
