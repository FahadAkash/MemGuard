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
/// YARA rule match diagnostic
/// </summary>
/// <param name="Matches">List of YARA rule matches</param>
/// <param name="TotalRulesScanned">Total number of rules scanned</param>
/// <param name="ScanDuration">Time taken for scan</param>
public record YaraDiagnostic(
    IReadOnlyList<YaraMatch> Matches,
    int TotalRulesScanned,
    TimeSpan ScanDuration) : DiagnosticBase("YARA", $"Found {Matches.Count} YARA rule matches", Matches.Count > 0 ? SeverityLevel.Critical : SeverityLevel.Info);

/// <summary>
/// Individual YARA rule match
/// </summary>
public record YaraMatch(
    string RuleName,
    string Namespace,
    IReadOnlyList<string> Tags,
    string Severity,
    IReadOnlyList<string> MatchedStrings);

/// <summary>
/// Indicator of Compromise detection diagnostic
/// </summary>
/// <param name="Indicators">Detected IOC indicators</param>
/// <param name="ThreatScore">Overall threat assessment score (0-100)</param>
public record IocDiagnostic(
    IReadOnlyList<IocMatch> Indicators,
    int ThreatScore) : DiagnosticBase("IOC", $"Detected {Indicators.Count} indicators of compromise", Indicators.Count > 0 ? SeverityLevel.Critical : SeverityLevel.Info);

/// <summary>
/// Individual IOC match
/// </summary>
public record IocMatch(
    string Type, // "IP", "Domain", "FilePath", "Registry", "Mutex"
    string Value,
    string ThreatLevel, // "Critical", "High", "Medium", "Low"
    string Description);

/// <summary>
/// Exploit technique detection diagnostic
/// </summary>
/// <param name="Techniques">Detected exploitation techniques</param>
/// <param name="OverallRiskScore">Overall risk assessment (0.0-1.0)</param>
public record ExploitDiagnostic(
    IReadOnlyList<ExploitTechnique> Techniques,
    double OverallRiskScore) : DiagnosticBase("Exploit", $"Detected {Techniques.Count} potential exploitation techniques", Techniques.Count > 0 ? SeverityLevel.Critical : SeverityLevel.Info);

/// <summary>
/// Individual exploit technique
/// </summary>
public record ExploitTechnique(
    string Name, // "ROP Chain", "Shellcode", "ETW Tampering", "Debugger Evasion"
    string Description,
    double Confidence,
    IReadOnlyList<string> Evidence);

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

// Memory analysis models
public class LeakReport
{
    public ulong TotalHeapSize { get; set; }
    public ulong TotalObjects { get; set; }
    public List<LeakTypeSummary> TopTypesByRetainedSize { get; set; } = new();
}

public class LeakTypeSummary
{
    public string TypeName { get; set; } = string.Empty;
    public ulong RetainedSize { get; set; }
    public int InstanceCount { get; set; }
    public List<ulong> ExampleObjectAddresses { get; set; } = new();
}

public record ObjectInfo(
    ulong Address,
    string TypeName,
    ulong Size,
    int Generation);

public record RetentionPath(
    ulong ObjectAddress,
    IReadOnlyList<string> PathFromRoot);

public class AnalysisOptions
{
    public int TopN { get; set; } = 30;
    public bool CalculateAccurateRetainedSize { get; set; } = true;
    public int MaxSamplesPerType { get; set; } = 5;
    public int AccurateRetainedSizeTopCount { get; set; } = 10;
}