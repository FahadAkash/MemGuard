using System.Text;
using System.Text.RegularExpressions;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using MemGuard.Core.Interfaces;
using MemGuard.Core.Models;
using Spectre.Console;

namespace MemGuard.Infrastructure;

/// <summary>
/// Applies code fixes based on AI suggestions
/// </summary>
public class CodeFixer : ICodeFixer
{
    private readonly IBackupManager _backupManager;

    public CodeFixer(IBackupManager backupManager)
    {
        _backupManager = backupManager;
    }

    public async Task<FixResult> ApplyFixesAsync(string aiResponse, string projectPath, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        var fixes = ParseFixes(aiResponse, projectPath);
        var errors = new List<string>();
        var appliedFixes = new List<CodeFix>();
        string? backupId = null;

        if (fixes.Count == 0)
        {
            errors.Add("No code fixes found in AI response");
            return new FixResult(false, appliedFixes, errors, null);
        }

        try
        {
            // Create backup before applying fixes
            if (!dryRun)
            {
                var filesToBackup = fixes.Select(f => f.FilePath).Distinct();
                backupId = await _backupManager.CreateBackupAsync(filesToBackup, "Auto-fix backup");
            }

            foreach (var fix in fixes)
            {
                try
                {
                    if (dryRun)
                    {
                        appliedFixes.Add(fix);
                    }
                    else
                    {
                        await ApplyFixAsync(fix, cancellationToken);
                        appliedFixes.Add(fix);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to apply fix to {fix.FilePath}: {ex.Message}");
                }
            }

            return new FixResult(errors.Count == 0, appliedFixes, errors, backupId);
        }
        catch (Exception ex)
        {
            errors.Add($"Fix operation failed: {ex.Message}");
            return new FixResult(false, appliedFixes, errors, backupId);
        }
    }

    private List<CodeFix> ParseFixes(string aiResponse, string projectPath)
    {
        var fixes = new List<CodeFix>();

        // Pattern to match code blocks with file paths
        var filePattern = @"(?:File|Path):\s*([^\n]+)";
        var codeBlockPattern = @"```(?:csharp|cs|diff)?\s*\n(.*?)\n```";

        var fileMatches = Regex.Matches(aiResponse, filePattern, RegexOptions.IgnoreCase);
        var codeMatches = Regex.Matches(aiResponse, codeBlockPattern, RegexOptions.Singleline);

        AnsiConsole.MarkupLine($"[grey]DEBUG: Found {fileMatches.Count} file matches and {codeMatches.Count} code blocks[/]");

        for (int i = 0; i < Math.Min(fileMatches.Count, codeMatches.Count); i++)
        {
            var rawPath = fileMatches[i].Groups[1].Value.Trim();
            // Clean up markdown formatting like **File.cs** or `File.cs`
            var filePath = rawPath.Trim('*', '`', ' ', '\r', '\n');
            
            var newContent = codeMatches[i].Groups[1].Value;

            AnsiConsole.MarkupLine($"[grey]DEBUG: Processing match {i+1}: {filePath}[/]");

            // Make path absolute
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(projectPath, filePath);
            }

            if (!File.Exists(filePath))
            {
                AnsiConsole.MarkupLine($"[yellow]DEBUG: File not found: {filePath}[/]");
                // Try to find the file recursively in the project if exact path fails
                var fileName = Path.GetFileName(filePath);
                var foundFiles = Directory.GetFiles(projectPath, fileName, SearchOption.AllDirectories);
                if (foundFiles.Length == 1)
                {
                    filePath = foundFiles[0];
                    AnsiConsole.MarkupLine($"[green]DEBUG: Found file at: {filePath}[/]");
                }
                else
                {
                    continue;
                }
            }

            try 
            {
                var originalContent = File.ReadAllText(filePath);
                var diff = DiffFormatter.GenerateColorizedDiff(originalContent, newContent, Path.GetFileName(filePath));
                var stats = DiffFormatter.GetChangeStatistics(originalContent, newContent);

                fixes.Add(new CodeFix(
                    filePath,
                    originalContent,
                    newContent,
                    diff,
                    1,
                    originalContent.Split('\n').Length,
                    stats.LinesAdded,
                    stats.LinesRemoved,
                    stats.LinesModified));
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]DEBUG: Error processing fix for {filePath}: {ex.Message}[/]");
            }
        }

        return fixes;
    }

    private async Task ApplyFixAsync(CodeFix fix, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(fix.FilePath, fix.NewContent, cancellationToken);
    }

    private string GenerateUnifiedDiff(string oldText, string newText, string fileName)
    {
        var diffBuilder = new InlineDiffBuilder(new Differ());
        var diff = diffBuilder.BuildDiffModel(oldText, newText);

        var sb = new StringBuilder();
        sb.AppendLine($"--- {fileName}");
        sb.AppendLine($"+++ {fileName}");

        int lineNumber = 1;
        foreach (var line in diff.Lines)
        {
            switch (line.Type)
            {
                case ChangeType.Deleted:
                    sb.AppendLine($"-{line.Text}");
                    break;
                case ChangeType.Inserted:
                    sb.AppendLine($"+{line.Text}");
                    break;
                case ChangeType.Modified:
                    sb.AppendLine($"-{line.Text}");
                    break;
                case ChangeType.Imaginary:
                    break;
                default:
                    sb.AppendLine($" {line.Text}");
                    break;
            }
            lineNumber++;
        }

        return sb.ToString();
    }
}
