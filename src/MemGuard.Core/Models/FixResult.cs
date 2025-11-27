namespace MemGuard.Core.Models;

/// <summary>
/// Result of applying code fixes
/// </summary>
public record FixResult(
    bool Success,
    IReadOnlyList<CodeFix> AppliedFixes,
    IReadOnlyList<string> Errors,
    string? BackupId);
