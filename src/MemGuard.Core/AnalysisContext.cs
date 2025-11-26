namespace MemGuard.Core;

/// <summary>
/// Context containing data needed for analysis
/// </summary>
public class AnalysisContext
{
    /// <summary>
    /// Path to the memory dump file
    /// </summary>
    public string DumpPath { get; init; } = string.Empty;
    
    /// <summary>
    /// Raw dump data (if loaded in memory)
    /// </summary>
    public byte[]? DumpData { get; init; }
    
    /// <summary>
    /// Additional configuration for analysis
    /// </summary>
    public IDictionary<string, string> Configuration { get; init; } = new Dictionary<string, string>();
}