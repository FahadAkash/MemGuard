namespace MemGuard.Core;

/// <summary>
/// Contract for report generators that format analysis results
/// </summary>
public interface IReporter
{
    /// <summary>
    /// Gets the format supported by this reporter
    /// </summary>
    string Format { get; }
    
    /// <summary>
    /// Generates a report from analysis results
    /// </summary>
    /// <param name="result">Analysis results to format</param>
    /// <param name="outputPath">Path to save the report</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the generated report</returns>
    Task<string> GenerateReportAsync(AnalysisResult result, string outputPath, CancellationToken cancellationToken = default);
}