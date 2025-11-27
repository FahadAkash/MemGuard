using MemGuard.Core;

namespace MemGuard.Core.Interfaces;

/// <summary>
/// Interface for performing complete memory leak analysis workflows
/// </summary>
public interface IMemoryAnalysisService
{
    /// <summary>
    /// Analyze a memory dump file and display the results
    /// </summary>
    /// <param name="dumpPath">Path to the dump file</param>
    /// <param name="options">Analysis options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    void AnalyzeDumpFile(string dumpPath, AnalysisOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attach to a live process and analyze it
    /// </summary>
    /// <param name="processId">Process ID to attach to</param>
    /// <param name="options">Analysis options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    void AnalyzeLiveProcess(int processId, AnalysisOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find and display retention path for a specific object
    /// </summary>
    /// <param name="dumpPath">Path to the dump file</param>
    /// <param name="objectAddress">Memory address of the object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    void FindObjectRetentionPath(string dumpPath, ulong objectAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze a specific object in detail
    /// </summary>
    /// <param name="dumpPath">Path to the dump file</param>
    /// <param name="objectAddress">Memory address of the object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    void AnalyzeSpecificObject(string dumpPath, ulong objectAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform quick heap analysis (faster, less detailed)
    /// </summary>
    /// <param name="dumpPath">Path to the dump file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    void QuickAnalysis(string dumpPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform deep heap analysis (slower, more detailed)
    /// </summary>
    /// <param name="dumpPath">Path to the dump file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    void DeepAnalysis(string dumpPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compare two dump files to see memory growth
    /// </summary>
    /// <param name="dumpPath1">Path to first dump file</param>
    /// <param name="dumpPath2">Path to second dump file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    void CompareDumps(string dumpPath1, string dumpPath2, CancellationToken cancellationToken = default);
}