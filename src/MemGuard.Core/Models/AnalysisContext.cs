using Microsoft.Diagnostics.Runtime;

namespace MemGuard.Core.Models;

/// <summary>
/// Context holding the state of the current analysis session
/// </summary>
public class AnalysisContext
{
    public string DumpPath { get; }
    public ClrRuntime Runtime { get; }
    public CancellationToken CancellationToken { get; }
    public List<DiagnosticBase> Diagnostics { get; } = new();

    public AnalysisContext(string dumpPath, ClrRuntime runtime, CancellationToken cancellationToken)
    {
        DumpPath = dumpPath;
        Runtime = runtime;
        CancellationToken = cancellationToken;
    }
}
