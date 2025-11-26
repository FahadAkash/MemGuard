using System.Collections.Immutable;

namespace MemGuard.Core;

/// <summary>
/// Analyzer for heap-related diagnostics
/// </summary>
public class HeapAnalyzer : IAnalyzer
{
    public string Name => "Heap Analyzer";

    public IImmutableList<DiagnosticBase> Analyze(AnalysisContext context)
    {
        // In a real implementation, this would analyze the actual dump
        // For now, we'll return sample data to demonstrate the pattern
        
        var diagnostics = new List<DiagnosticBase>
        {
            new HeapDiagnostic(
                FragmentationLevel: 0.35,
                LargestFreeBlock: 1024 * 1024 * 5, // 5 MB
                TotalSize: 1024 * 1024 * 100) // 100 MB
        };

        return diagnostics.ToImmutableList();
    }
}