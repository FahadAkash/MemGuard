using MemGuard.Core;
using Microsoft.Diagnostics.Runtime;

namespace MemGuard.Plugins;

/// <summary>
/// Plugin interface for custom memory dump detectors
/// </summary>
public interface IDetectorPlugin
{
    /// <summary>
    /// Unique name of the detector plugin
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin version
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Description of what this detector does
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Analyze memory dump and return diagnostic findings
    /// </summary>
    /// <param name="runtime">CLR runtime from the dump</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Diagnostic result or null if no issues found</returns>
    Task<DiagnosticBase?> AnalyzeAsync(ClrRuntime runtime, CancellationToken cancellationToken = default);
}
