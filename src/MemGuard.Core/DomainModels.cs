namespace MemGuard.Core;

/// <summary>
/// Represents the result of a memory dump analysis
/// </summary>
/// <param name="RootCause">Plain-English explanation of the issue</param>
/// <param name="CodeFix">Diff-style code fix with exact line numbers</param>
/// <param name="ConfidenceScore">Confidence level of the analysis (0.0 to 1.0)</param>
/// <param name="Diagnostics">List of diagnostic findings</param>
public record AnalysisResult(
    string RootCause,
    string CodeFix,
    double ConfidenceScore,
    IReadOnlyList<DiagnosticBase> Diagnostics);

/// <summary>
/// Base class for all diagnostic findings
/// </summary>
/// <param name="Type">Type of diagnostic</param>
/// <param name="Description">Description of the finding</param>
/// <param name="Severity">Severity level</param>
public abstract record DiagnosticBase(
    string Type,
    string Description,
    SeverityLevel Severity);

/// <summary>
/// Heap-related diagnostic information
/// </summary>
/// <param name="FragmentationLevel">Level of heap fragmentation</param>
/// <param name="LargestFreeBlock">Size of largest free block</param>
/// <param name="TotalSize">Total heap size</param>
public record HeapDiagnostic(
    double FragmentationLevel,
    long LargestFreeBlock,
    long TotalSize) : DiagnosticBase("Heap", $"Heap fragmentation: {FragmentationLevel:P2}", SeverityLevel.Warning);

/// <summary>
/// Deadlock-related diagnostic information
/// </summary>
/// <param name="ThreadIds">IDs of threads involved in deadlock</param>
/// <param name="LockObjects">Objects being contested</param>
public record DeadlockDiagnostic(
    IReadOnlyList<int> ThreadIds,
    IReadOnlyList<string> LockObjects) : DiagnosticBase("Deadlock", $"Deadlock detected between threads {string.Join(", ", ThreadIds)}", SeverityLevel.Critical);

/// <summary>
/// Pinned object diagnostic information
/// </summary>
/// <param name="PinnedObjectCount">Number of pinned objects</param>
/// <param name="GcPressureLevel">GC pressure level caused by pinned objects</param>
public record PinnedObjectDiagnostic(
    int PinnedObjectCount,
    string GcPressureLevel) : DiagnosticBase("PinnedObjects", $"Found {PinnedObjectCount} pinned objects causing {GcPressureLevel} GC pressure", SeverityLevel.Warning);

/// <summary>
/// Async state machine corruption diagnostic
/// </summary>
/// <param name="StateMachineId">ID of corrupted state machine</param>
/// <param name="StackTrace">Stack trace of corruption</param>
public record AsyncCorruptionDiagnostic(
    string StateMachineId,
    string StackTrace) : DiagnosticBase("AsyncCorruption", $"Async state machine {StateMachineId} shows signs of corruption", SeverityLevel.Error);

/// <summary>
/// Severity levels for diagnostics
/// </summary>
public enum SeverityLevel
{
    Info,
    Warning,
    Error,
    Critical
}