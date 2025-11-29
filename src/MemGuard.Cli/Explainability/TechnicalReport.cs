using MemGuard.Core;
using Spectre.Console;
using System.Text;

namespace MemGuard.Cli.Explainability;

/// <summary>
/// Generates detailed technical reports for engineers with full stack traces and diagnostics
/// </summary>
public class TechnicalReport
{
    public void RenderDetailedReport(AnalysisResult result, string dumpPath)
    {
        AnsiConsole.Clear();
        
        // Header
        RenderHeader(dumpPath);

        // Root cause analysis
        RenderRootCauseAnalysis(result);

        // Detailed diagnostics
        RenderDetailedDiagnostics(result);

        // Code fixes
        if (!string.IsNullOrEmpty(result.CodeFix))
        {
            RenderCodeFixes(result.CodeFix);
        }

        // Footer with metadata
        RenderFooter(result);
    }

    private void RenderHeader(string dumpPath)
    {
        var figlet = new FigletText("MemGuard Analysis")
            .Centered()
            .Color(Color.Cyan1);

        AnsiConsole.Write(figlet);
        
        AnsiConsole.Write(new Rule($"[grey]Dump: {Path.GetFileName(dumpPath)}[/]")
            .RuleStyle(Style.Parse("grey")));
        
        AnsiConsole.WriteLine();
    }

    private void RenderRootCauseAnalysis(AnalysisResult result)
    {
        AnsiConsole.Write(new Rule("[bold yellow]Root Cause Analysis[/]")
            .RuleStyle(Style.Parse("yellow")));
        
        AnsiConsole.WriteLine();

        var panel = new Panel(result.RootCause)
            .Header("[bold]Diagnosis[/]")
            .BorderColor(Color.Yellow)
            .Padding(1, 1);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Confidence indicator
        var confidenceBar = GenerateConfidenceBar(result.ConfidenceScore);
        AnsiConsole.MarkupLine($"Confidence: {confidenceBar} {result.ConfidenceScore:P0}");
        AnsiConsole.WriteLine();
    }

    private void RenderDetailedDiagnostics(AnalysisResult result)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Detailed Diagnostics[/]")
            .RuleStyle(Style.Parse("cyan")));
        
        AnsiConsole.WriteLine();

        if (!result.Diagnostics.Any())
        {
            AnsiConsole.MarkupLine("[grey]No specific diagnostics found.[/]");
            return;
        }

        foreach (var diagnostic in result.Diagnostics)
        {
            RenderDiagnostic(diagnostic);
        }
    }

    private void RenderDiagnostic(DiagnosticBase diagnostic)
    {
        var severityColor = diagnostic.Severity switch
        {
            SeverityLevel.Critical => Color.Red,
            SeverityLevel.Error => Color.Orange1,
            SeverityLevel.Warning => Color.Yellow,
            _ => Color.Grey
        };

        var header = $"[bold]{diagnostic.Type}[/] - [{severityColor}]{diagnostic.Severity}[/]";
        
        var content = new StringBuilder();
        content.AppendLine(diagnostic.Description);
        content.AppendLine();

        // Type-specific rendering
        switch (diagnostic)
        {
            case DeadlockDiagnostic deadlock:
                content.AppendLine($"[bold]Threads Involved:[/] {string.Join(", ", deadlock.ThreadIds)}");
                content.AppendLine($"[bold]Lock Objects:[/]");
                foreach (var lockObj in deadlock.LockObjects)
                {
                    content.AppendLine($"  • {lockObj}");
                }
                break;

            case HeapDiagnostic heap:
                content.AppendLine($"[bold]Fragmentation Level:[/] {heap.FragmentationLevel:P2}");
                content.AppendLine($"[bold]Largest Free Block:[/] {FormatBytes(heap.LargestFreeBlock)}");
                content.AppendLine($"[bold]Total Heap Size:[/] {FormatBytes(heap.TotalSize)}");
                break;

            case YaraDiagnostic yara:
                content.AppendLine($"[bold]Rules Scanned:[/] {yara.TotalRulesScanned}");
                content.AppendLine($"[bold]Scan Duration:[/] {yara.ScanDuration.TotalMilliseconds:F0}ms");
                if (yara.Matches.Any())
                {
                    content.AppendLine($"[bold red]Matches Found:[/]");
                    foreach (var match in yara.Matches.Take(5))
                    {
                        content.AppendLine($"  [red]•[/] {match.RuleName} ({match.Severity})");
                    }
                }
                break;

            case IocDiagnostic ioc:
                content.AppendLine($"[bold]Threat Score:[/] {ioc.ThreatScore}/100");
                if (ioc.Indicators.Any())
                {
                    content.AppendLine($"[bold red]Indicators Found:[/]");
                    foreach (var indicator in ioc.Indicators.Take(10))
                    {
                        var color = indicator.ThreatLevel == "Critical" ? "red" :
                                   indicator.ThreatLevel == "High" ? "orange1" :
                                   indicator.ThreatLevel == "Medium" ? "yellow" : "grey";
                        content.AppendLine($"  [{color}]•[/] [{color}]{indicator.Type}:[/] {indicator.Value}");
                        content.AppendLine($"    {indicator.Description}");
                    }
                }
                break;

            case ExploitDiagnostic exploit:
                content.AppendLine($"[bold]Overall Risk Score:[/] {exploit.OverallRiskScore:P0}");
                if (exploit.Techniques.Any())
                {
                    content.AppendLine($"[bold red]Techniques Detected:[/]");
                    foreach (var technique in exploit.Techniques)
                    {
                        content.AppendLine($"  [red]•[/] [bold]{technique.Name}[/] (Confidence: {technique.Confidence:P0})");
                        content.AppendLine($"    {technique.Description}");
                        if (technique.Evidence.Any())
                        {
                            content.AppendLine($"    Evidence:");
                            foreach (var evidence in technique.Evidence.Take(3))
                            {
                                content.AppendLine($"      - {evidence}");
                            }
                        }
                    }
                }
                break;
        }

        var panel = new Panel(content.ToString())
            .Header(header)
            .BorderColor(severityColor)
            .Padding(1, 0);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private void RenderCodeFixes(string codeFix)
    {
        AnsiConsole.Write(new Rule("[bold green]Suggested Code Fixes[/]")
            .RuleStyle(Style.Parse("green")));
        
        AnsiConsole.WriteLine();

        var panel = new Panel(new Text(codeFix))
            .Header("[bold]Recommended Changes[/]")
            .BorderColor(Color.Green)
            .Padding(1, 1);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private void RenderFooter(AnalysisResult result)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        AnsiConsole.Write(new Rule("[grey]Analysis Metadata[/]")
            .RuleStyle(Style.Parse("grey")));
        
        AnsiConsole.WriteLine();
        
        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn("")
            .AddColumn("");

        table.AddRow("[grey]Generated:[/]", timestamp);
        table.AddRow("[grey]Total Diagnostics:[/]", result.Diagnostics.Count.ToString());
        table.AddRow("[grey]Confidence Score:[/]", $"{result.ConfidenceScore:P1}");
        table.AddRow("[grey]Critical Issues:[/]", result.Diagnostics.Count(d => d.Severity == SeverityLevel.Critical).ToString());
        table.AddRow("[grey]Errors:[/]", result.Diagnostics.Count(d => d.Severity == SeverityLevel.Error).ToString());
        table.AddRow("[grey]Warnings:[/]", result.Diagnostics.Count(d => d.Severity == SeverityLevel.Warning).ToString());

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private string GenerateConfidenceBar(double confidence)
    {
        var barWidth = 20;
        var filled = (int)(confidence * barWidth);
        var empty = barWidth - filled;

        var color = confidence >= 0.8 ? "green" :
                   confidence >= 0.5 ? "yellow" : "red";

        return $"[{color}]{new string('█', filled)}[/][grey]{new string('░', empty)}[/]";
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
