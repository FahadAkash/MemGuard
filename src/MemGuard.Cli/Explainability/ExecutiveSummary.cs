using MemGuard.Core;
using Spectre.Console;

namespace MemGuard.Cli.Explainability;

/// <summary>
/// Generates plain-English executive summaries for non-technical stakeholders
/// </summary>
public class ExecutiveSummary
{
    public string GenerateSummary(AnalysisResult result)
    {
        var summary = new System.Text.StringBuilder();
        
        // Executive-level summary (3-5 sentences, no jargon)
        summary.AppendLine(FormatForExecutives(result));
        
        return summary.ToString();
    }

    public void RenderExecutiveSummary(AnalysisResult result)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold blue]Executive Summary[/]").RuleStyle(Style.Parse("blue")));
        AnsiConsole.WriteLine();

        var panel = new Panel(GenerateSummary(result))
            .Header("[bold]Analysis Overview[/]")
            .BorderColor(Color.Blue)
            .Padding(1, 1);

        AnsiConsole.Write(panel);

        // Business impact assessment
        RenderBusinessImpact(result);

        // Recommended actions
        RenderRecommendedActions(result);
    }

    private string FormatForExecutives(AnalysisResult result)
    {
        var text = new System.Text.StringBuilder();
        
        // Severity assessment
        var severity = DetermineSeverity(result);
        var severityColor = severity switch
        {
            "Critical" => "red",
            "High" => "orange1",
            "Medium" => "yellow",
            _ => "green"
        };

        text.AppendLine($"[bold {severityColor}]Severity: {severity}[/]");
        text.AppendLine();

        // Simple explanation
        text.AppendLine(SimplifyExplanation(result.RootCause));
        text.AppendLine();

        // Confidence level
        var confidenceText = result.ConfidenceScore >= 0.8 ? "high confidence" :
                            result.ConfidenceScore >= 0.5 ? "moderate confidence" :
                            "low confidence";
        text.AppendLine($"This assessment is made with [bold]{confidenceText}[/] ({result.ConfidenceScore:P0}).");

        return text.ToString();
    }

    private void RenderBusinessImpact(AnalysisResult result)
    {
        var impact = AssessBusinessImpact(result);
        
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Impact Area[/]")
            .AddColumn("[bold]Assessment[/]");

        foreach (var (area, assessment) in impact)
        {
            table.AddRow($"[cyan]{area}[/]", assessment);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private void RenderRecommendedActions(AnalysisResult result)
    {
        AnsiConsole.Write(new Rule("[bold]Recommended Actions[/]"));
        AnsiConsole.WriteLine();

        var actions = GetRecommendedActions(result);
        
        for (int i = 0; i < actions.Count; i++)
        {
            AnsiConsole.MarkupLine($"[bold cyan]{i + 1}.[/] {actions[i]}");
        }

        AnsiConsole.WriteLine();
    }

    private string SimplifyExplanation(string technicalExplanation)
    {
        // Convert technical jargon to plain English
        var simplified = technicalExplanation
            .Replace("deadlock", "a situation where the application stopped responding because different parts were waiting for each other")
            .Replace("memory leak", "the application is using more memory than it should and not releasing it")
            .Replace("heap fragmentation", "memory is not being used efficiently, leading to waste")
            .Replace("thread", "parallel task")
            .Replace("stack trace", "sequence of operations")
            .Replace("CLR", "the .NET runtime");

        // Keep it concise
        var sentences = simplified.Split(new[] { ". " }, StringSplitOptions.None);
        var summary = string.Join(". ", sentences.Take(3));
        
        return summary + (sentences.Length > 3 ? "..." : "");
    }

    private string DetermineSeverity(AnalysisResult result)
    {
        // Determine severity based on diagnostics
        var hasCritical = result.Diagnostics.Any(d => d.Severity == SeverityLevel.Critical);
        var hasError = result.Diagnostics.Any(d => d.Severity == SeverityLevel.Error);

        if (hasCritical) return "Critical";
        if (hasError) return "High";
        if (result.Diagnostics.Any(d => d.Severity == SeverityLevel.Warning)) return "Medium";
        return "Low";
    }

    private List<(string Area, string Assessment)> AssessBusinessImpact(AnalysisResult result)
    {
        var impact = new List<(string, string)>();

        // User Experience
        if (result.Diagnostics.Any(d => d.Type == "Deadlock"))
        {
            impact.Add(("User Experience", "[red]High Impact - Application hangs/freezes[/]"));
        }
        else if (result.Diagnostics.Any(d => d.Type == "Heap"))
        {
            impact.Add(("User Experience", "[yellow]Medium Impact - Possible slowdowns[/]"));
        }
        else
        {
            impact.Add(("User Experience", "[green]Low Impact[/]"));
        }

        // Resource Costs
        if (result.Diagnostics.Any(d => d.Description.Contains("memory")))
        {
            impact.Add(("Resource Costs", "[yellow]Increased memory usage may raise cloud costs[/]"));
        }

        // Security
        if (result.Diagnostics.Any(d => d.Type == "IOC" || d.Type == "YARA" || d.Type == "Exploit"))
        {
            impact.Add(("Security Posture", "[red]⚠ Potential security threat detected[/]"));
        }

        // Reliability
        impact.Add(("System Reliability", 
            result.ConfidenceScore > 0.7 ? "[yellow]Needs attention[/]" : "[green]Stable[/]"));

        return impact;
    }

    private List<string> GetRecommendedActions(AnalysisResult result)
    {
        var actions = new List<string>();

        // Immediate actions
        if (result.Diagnostics.Any(d => d.Severity == SeverityLevel.Critical))
        {
            actions.Add("[red bold]IMMEDIATE:[/] Review and apply the suggested code fixes below");
            actions.Add("[red]IMMEDIATE:[/] Consider rolling back recent deployments if this is production");
        }

        // Short-term actions
        actions.Add("Schedule a review meeting with the development team");
        
        if (result.Diagnostics.Any(d => d.Type == "Heap"))
        {
            actions.Add("Conduct memory profiling to identify optimization opportunities");
        }

        if (result.Diagnostics.Any(d => d.Type == "Deadlock"))
        {
            actions.Add("Review and refactor synchronization logic in the affected components");
        }

        // Security actions
        if (result.Diagnostics.Any(d => d.Type == "IOC" || d.Type == "Exploit"))
        {
            actions.Add("[red]URGENT:[/] Escalate to security team for immediate investigation");
            actions.Add("Isolate affected systems until security review is complete");
        }

        // Long-term actions
        actions.Add("Implement automated memory analysis in your CI/CD pipeline");
        actions.Add("Consider adding performance and memory usage monitoring to production");

        return actions;
    }
}
