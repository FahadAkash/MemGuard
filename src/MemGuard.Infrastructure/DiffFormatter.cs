using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Spectre.Console;

namespace MemGuard.Infrastructure;

/// <summary>
/// Formats diffs with color-coding and context for better readability
/// </summary>
public static class DiffFormatter
{
    /// <summary>
    /// Generates a colorized diff showing only changed sections with context
    /// </summary>
    /// <param name="oldText">Original text</param>
    /// <param name="newText">New text</param>
    /// <param name="fileName">File name for header</param>
    /// <param name="contextLines">Number of context lines to show around changes (default: 3)</param>
    /// <returns>Colorized diff markup for Spectre.Console</returns>
    public static string GenerateColorizedDiff(string oldText, string newText, string fileName, int contextLines = 3)
    {
        var diffBuilder = new InlineDiffBuilder(new Differ());
        var diff = diffBuilder.BuildDiffModel(oldText, newText);

        var hunks = ExtractHunks(diff.Lines, contextLines);
        
        if (hunks.Count == 0)
        {
            return "[grey]No changes detected[/]";
        }

        return FormatHunks(hunks, fileName);
    }

    /// <summary>
    /// Generates change statistics from a diff
    /// </summary>
    public static (int LinesAdded, int LinesRemoved, int LinesModified) GetChangeStatistics(string oldText, string newText)
    {
        var diffBuilder = new InlineDiffBuilder(new Differ());
        var diff = diffBuilder.BuildDiffModel(oldText, newText);

        int added = 0, removed = 0, modified = 0;

        foreach (var line in diff.Lines)
        {
            switch (line.Type)
            {
                case ChangeType.Inserted:
                    added++;
                    break;
                case ChangeType.Deleted:
                    removed++;
                    break;
                case ChangeType.Modified:
                    modified++;
                    break;
            }
        }

        return (added, removed, modified);
    }

    /// <summary>
    /// Renders a diff table showing file changes with statistics
    /// </summary>
    public static Table CreateDiffSummaryTable(List<(string FilePath, int Added, int Removed, int Modified)> changes)
    {
        var table = new Table();
        table.AddColumn(new TableColumn("[yellow]File[/]").LeftAligned());
        table.AddColumn(new TableColumn("[green]+[/]").RightAligned());
        table.AddColumn(new TableColumn("[red]-[/]").RightAligned());
        table.AddColumn(new TableColumn("[blue]~[/]").RightAligned());

        foreach (var (filePath, added, removed, modified) in changes)
        {
            var fileName = Path.GetFileName(filePath);
            table.AddRow(
                fileName.EscapeMarkup(),
                $"[green]{added}[/]",
                $"[red]{removed}[/]",
                $"[blue]{modified}[/]"
            );
        }

        return table;
    }

    private static List<DiffHunk> ExtractHunks(IReadOnlyList<DiffPiece> lines, int contextLines)
    {
        var hunks = new List<DiffHunk>();
        DiffHunk? currentHunk = null;
        int lineNumber = 1;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            
            if (line.Type != ChangeType.Unchanged)
            {
                // Start a new hunk if needed
                if (currentHunk == null)
                {
                    int startLine = Math.Max(1, lineNumber - contextLines);
                    currentHunk = new DiffHunk { StartLine = startLine };
                    
                    // Add context before the change
                    for (int j = Math.Max(0, i - contextLines); j < i; j++)
                    {
                        currentHunk.Lines.Add((lines[j], startLine + (j - i + contextLines)));
                    }
                }

                currentHunk.Lines.Add((line, lineNumber));
            }
            else if (currentHunk != null)
            {
                // Add context after the change
                currentHunk.Lines.Add((line, lineNumber));
                
                // Check if we have enough context lines
                int unchangedCount = 0;
                for (int j = currentHunk.Lines.Count - 1; j >= 0; j--)
                {
                    if (currentHunk.Lines[j].Item1.Type == ChangeType.Unchanged)
                        unchangedCount++;
                    else
                        break;
                }

                if (unchangedCount >= contextLines)
                {
                    // Trim excess context and finalize hunk
                    while (unchangedCount > contextLines)
                    {
                        currentHunk.Lines.RemoveAt(currentHunk.Lines.Count - 1);
                        unchangedCount--;
                    }
                    
                    currentHunk.EndLine = lineNumber;
                    hunks.Add(currentHunk);
                    currentHunk = null;
                }
            }

            if (line.Type != ChangeType.Imaginary)
            {
                lineNumber++;
            }
        }

        // Finalize any remaining hunk
        if (currentHunk != null)
        {
            currentHunk.EndLine = lineNumber - 1;
            hunks.Add(currentHunk);
        }

        return hunks;
    }

    private static string FormatHunks(List<DiffHunk> hunks, string fileName)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"[grey]━━━ {fileName.EscapeMarkup()} ━━━[/]");

        foreach (var hunk in hunks)
        {
            sb.AppendLine($"[cyan]@@ Lines {hunk.StartLine}-{hunk.EndLine} @@[/]");
            
            foreach (var (line, lineNum) in hunk.Lines)
            {
                var lineNumStr = lineNum.ToString().PadLeft(4);
                var prefix = line.Type switch
                {
                    ChangeType.Inserted => $"[green]{lineNumStr} +[/]",
                    ChangeType.Deleted => $"[red]{lineNumStr} -[/]",
                    ChangeType.Modified => $"[yellow]{lineNumStr} ~[/]",
                    _ => $"[grey]{lineNumStr}  [/]"
                };

                var text = line.Text?.EscapeMarkup() ?? "";
                var coloredText = line.Type switch
                {
                    ChangeType.Inserted => $"[green]{text}[/]",
                    ChangeType.Deleted => $"[red]{text}[/]",
                    ChangeType.Modified => $"[yellow]{text}[/]",
                    _ => $"[grey]{text}[/]"
                };

                sb.AppendLine($"{prefix} {coloredText}");
            }
            
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private class DiffHunk
    {
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public List<(DiffPiece Line, int LineNumber)> Lines { get; } = new();
    }
}
