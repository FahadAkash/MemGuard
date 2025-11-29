# 🚀 MemGuard Performance Benchmarks - Quick Start

## Run Benchmarks

```bash
# Navigate to benchmarks
cd benchmarks/MemGuard.Benchmarks

# Run all benchmarks
dotnet run -c Release

# Run specific benchmark
dotnet run -c Release --filter "*ExtractorBenchmarks*"
dotnet run -c Release --filter "*VisualizationBenchmarks*"
```

## Expected Performance

| Component | Target | Typical |
|-----------|--------|---------|
| IOC Extraction | < 100ms | ~15ms |
| Exploit Detection | < 150ms | ~10ms |
| YARA Scanning | < 200ms | ~25ms |
| Stack Trace Render | < 20ms | ~6ms |
| Memory Map Render | < 30ms | ~8ms |

## All Commands

```bash
# Build benchmarks
dotnet build -c Release

# Run all
dotnet run -c Release

# Run and export HTML
dotnet run -c Release --exporters html

# Run and export Markdown
dotnet run -c Release --exporters markdown

# Run with memory diagnoser
dotnet run -c Release --memory

# Quick run (fewer iterations)
dotnet run -c Release --job short

# Detailed run (more iterations)
dotnet run -c Release --job long
```

## Interpret Results

- **Mean**: Average execution time
- **Error**: Margin of error
- **StdDev**: Consistency (lower is better)
- **Gen0/1/2**: Garbage collections (lower is better)
- **Allocated**: Memory used (lower is better)

See [Full Documentation](README.md) for details.
