using MemGuard.Core;

namespace MemGuard.Plugins;

/// <summary>
/// Plugin interface for custom exporters
/// </summary>
public interface IExporterPlugin
{
    /// <summary>
    /// Unique name of the exporter plugin
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin version
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Description of export format
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Supported file extensions (e.g., ".json", ".xml")
    /// </summary>
    string[] SupportedExtensions { get; }

    /// <summary>
    /// Export analysis result to specified format
    /// </summary>
    /// <param name="result">Analysis result to export</param>
    /// <param name="outputPath">Output file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExportAsync(AnalysisResult result, string outputPath, CancellationToken cancellationToken = default);
}
