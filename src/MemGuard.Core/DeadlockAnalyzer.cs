using System.Collections.Immutable;

namespace MemGuard.Core;

/// <summary>
/// Analyzer for deadlock-related diagnostics
/// </summary>
public class DeadlockAnalyzer : IAnalyzer
{
    public string Name => "Deadlock Analyzer";

    public IImmutableList<DiagnosticBase> Analyze(AnalysisContext context)
    {
        // In a real implementation, this would analyze the actual dump
        // For now, we'll return sample data to demonstrate the pattern
        
        var diagnostics = new List<DiagnosticBase>
        {
            new DeadlockDiagnostic(
                ThreadIds: new List<int> { 1234, 5678 },
                LockObjects: new List<string> { "System.Object@0x12345678", "System.Object@0x87654321" })
        };

        return diagnostics.ToImmutableList();
    }
}