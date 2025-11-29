# MemGuard Benchmarks

Performance benchmarking suite for MemGuard using BenchmarkDotNet.

## 🎯 What's Benchmarked

### Extractors
- **IOC Extractor** - Pattern matching performance
- **Exploit Heuristics** - Heuristic analysis speed
- **YARA Extractor** - Rule scanning throughput
- **Symbol Resolution** - Symbol loading speed

### Visualizations
- **Stack Trace Rendering** - ASCII art generation
- **Deadlock Graphs** - Cycle diagram creation
- **Memory Maps** - Chart and heat map rendering
- **Type Distribution** - Graph generation

## 🚀 Running Benchmarks

### Run All Benchmarks
```bash
cd benchmarks/MemGuard.Benchmarks
dotnet run -c Release
```

### Run Specific Benchmark
```bash
# Extractor benchmarks only
dotnet run -c Release --filter "*ExtractorBenchmarks*"

# Visualization benchmarks only
dotnet run -c Release --filter "*VisualizationBenchmarks*"

# Specific method
dotnet run -c Release --filter "*IocExtractor*"
```

### Generate Reports
```bash
# HTML report
dotnet run -c Release --exporters html

# Markdown report
dotnet run -c Release --exporters markdown

# CSV export
dotnet run -c Release --exporters csv
```

## 📊 Understanding Results

### Key Metrics

| Metric | Description | Good Target |
|--------|-------------|-------------|
| **Mean** | Average execution time | < 100ms for extractors |
| **StdDev** | Consistency of performance | Low variance |
| **Gen0/Gen1/Gen2** | Garbage collections | Minimize GC pressure |
| **Allocated** | Memory allocated | < 10 MB per operation |

### Example Output
```
| Method                          | Mean      | Error    | StdDev   | Allocated |
|-------------------------------- |----------:|---------:|---------:|----------:|
| IOC Extractor - Empty Heap      | 15.23 ms  | 0.21 ms  | 0.18 ms  | 2.1 MB    |
| Exploit Heuristics - Empty Heap | 8.45 ms   | 0.15 ms  | 0.13 ms  | 1.5 MB    |
| YARA Extractor - Empty Heap     | 22.10 ms  | 0.35 ms  | 0.30 ms  | 3.2 MB    |
| Render Stack Trace (20 frames)  | 5.67 ms   | 0.08 ms  | 0.07 ms  | 850 KB    |
```

## 🔍 Interpreting Performance

### Extractor Performance
- **Fast** (< 50ms): Good for real-time analysis
- **Moderate** (50-200ms): Acceptable for batch processing
- **Slow** (> 200ms): May need optimization

### Visualization Performance
- **Fast** (< 10ms): Instant rendering
- **Moderate** (10-50ms): Acceptable UX
- **Slow** (> 50ms): Noticeable delay

## 📈 Baseline Performance Targets

Based on typical .NET memory dumps:

| Operation | Target Time | Target Memory |
|-----------|-------------|---------------|
| IOC Scan (small dump) | < 100ms | < 5 MB |
| IOC Scan (large dump) | < 500ms | < 50 MB |
| Exploit Detection | < 150ms | < 10 MB |
| YARA Rules (10 rules) | < 200ms | < 15 MB |
| Stack Trace Render | < 20ms | < 2 MB |
| Memory Map Render | < 30ms | < 3 MB |

## 🛠️ Optimization Tips

### If Extractors Are Slow
1. **Profile with dotTrace** or **Visual Studio Profiler**
2. **Reduce LINQ overhead** - Use `for` loops for hot paths
3. **Cache regex patterns** - Compile once, reuse
4. **Early exit conditions** - Skip unnecessary work
5. **Parallel processing** - Use `Parallel.ForEach` for large datasets

### If Visualizations Are Slow
1. **Limit output size** - Cap at reasonable defaults
2. **Lazy rendering** - Only render visible content
3. **String builder** - Use `StringBuilder` for text generation
4. **Reduce console I/O** - Batch writes

### If Memory Is High
1. **Use structs** for small data
2. **Pool objects** with `ArrayPool<T>`
3. **Dispose resources** properly
4. **Avoid string allocations** - Use spans

## 🔬 Advanced Benchmarking

### Custom Parameters
```csharp
[Params(10, 100, 1000)]
public int ObjectCount;

[Benchmark]
public async Task<DiagnosticBase?> IocExtractor_WithObjects()
{
    // Setup mock with ObjectCount objects
    // Run extractor
}
```

### Memory Profiling
```csharp
[MemoryDiagnoser]
[ThreadingDiagnoser]
[HardwareCounters(HardwareCounter.CacheMisses)]
public class MyBenchmarks { }
```

### Comparing Implementations
```csharp
[Benchmark(Baseline = true)]
public void OldImplementation() { }

[Benchmark]
public void NewImplementation() { }
```

## 📊 Continuous Benchmarking

### In CI/CD
```yaml
# GitHub Actions example
- name: Run Benchmarks
  run: |
    cd benchmarks/MemGuard.Benchmarks
    dotnet run -c Release --exporters json
    
- name: Upload Results
  uses: actions/upload-artifact@v3
  with:
    name: benchmark-results
    path: BenchmarkDotNet.Artifacts/results/
```

### Track Over Time
```bash
# Run and save results
dotnet run -c Release --exporters json
mv BenchmarkDotNet.Artifacts/results/*.json results/$(date +%Y%m%d).json

# Compare with previous
dotnet run -c Release --filter "*" --baseline results/baseline.json
```

## 🎯 Performance Goals

### v1.0 Targets (Current)
- ✅ IOC Extraction: < 100ms
- ✅ Exploit Detection: < 150ms
- ✅ YARA Scanning: < 200ms
- ✅ Visualization: < 30ms

### v2.0 Goals
- 🎯 IOC Extraction: < 50ms (2x faster)
- 🎯 Parallel extraction for multi-core
- 🎯 Streaming YARA for large dumps
- 🎯 GPU-accelerated pattern matching

## 📝 Contributing Benchmarks

When adding new features, add corresponding benchmarks:

```csharp
[Benchmark(Description = "My New Feature")]
public void MyNewFeature()
{
    // Benchmark code
}
```

### Benchmark Checklist
- [ ] Add `[MemoryDiagnoser]` to measure allocations
- [ ] Use realistic data sizes
- [ ] Include both best-case and worst-case scenarios
- [ ] Document expected performance
- [ ] Run before and after optimization

## 🔗 Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [.NET Performance Tips](https://learn.microsoft.com/en-us/dotnet/framework/performance/)
- [Memory Profiling](https://learn.microsoft.com/en-us/visualstudio/profiling/)

---

**Benchmark often, optimize when needed!** 🚀
