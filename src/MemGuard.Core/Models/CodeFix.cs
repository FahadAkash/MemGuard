namespace MemGuard.Core.Models;

/// <summary>
/// Represents a code fix to be applied
/// </summary>
public record CodeFix(
    string FilePath,
    string OriginalContent,
    string NewContent,
    string UnifiedDiff,
    int StartLine,
    int EndLine,
    int LinesAdded = 0,
    int LinesRemoved = 0,
    int LinesModified = 0);
