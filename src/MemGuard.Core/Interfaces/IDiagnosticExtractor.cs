using MemGuard.Core.Models;
using Microsoft.Diagnostics.Runtime;

namespace MemGuard.Core.Interfaces;

/// <summary>
/// Strategy interface for extracting specific diagnostics from a memory dump
/// </summary>
public interface IDiagnosticExtractor
{
    /// <summary>
    /// Unique name of the extractor (e.g., "Heap", "Deadlock")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Extracts diagnostic information from the runtime
    /// </summary>
    /// <param name="runtime">ClrRuntime instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Diagnostic finding or null if no issue found</returns>
    Task<DiagnosticBase?> ExtractAsync(ClrRuntime runtime, CancellationToken cancellationToken = default);
}
