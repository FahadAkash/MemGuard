using MemGuard.Core.Models;

namespace MemGuard.Core.Interfaces;

/// <summary>
/// Interface for applying code fixes based on AI suggestions
/// </summary>
public interface ICodeFixer
{
    /// <summary>
    /// Parse and apply code fixes from AI response
    /// </summary>
    /// <param name="aiResponse">AI response containing fix suggestions</param>
    /// <param name="projectPath">Path to the project to fix</param>
    /// <param name="dryRun">If true, only show what would be changed without applying</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the fix operation</returns>
    Task<FixResult> ApplyFixesAsync(string aiResponse, string projectPath, bool dryRun = false, CancellationToken cancellationToken = default);
}
