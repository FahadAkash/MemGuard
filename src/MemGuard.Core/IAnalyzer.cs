using System.Collections.Immutable;

namespace MemGuard.Core;

/// <summary>
/// Contract for diagnostic analyzers that extract specific diagnostic signals from memory dumps
/// </summary>
public interface IAnalyzer
{
    /// <summary>
    /// Gets the name of the analyzer
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Analyzes a memory dump and extracts diagnostic information
    /// </summary>
    /// <param name="context">Analysis context containing dump data</param>
    /// <returns>Diagnostic findings</returns>
    IImmutableList<DiagnosticBase> Analyze(AnalysisContext context);
}