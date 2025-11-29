using Spectre.Console;
using Microsoft.Diagnostics.Runtime;

namespace MemGuard.Cli.Visualization;

/// <summary>
/// Renders crash stack traces as beautiful ASCII art trees
/// </summary>
public class CrashTraceVisualizer
{
    public void RenderStackTrace(IEnumerable<ClrStackFrame> frames, string threadInfo = "")
    {
        if (!frames.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No stack frames available[/]");
            return;
        }

        var tree = new Tree($"[bold cyan]Stack Trace[/] {threadInfo}")
            .Style(Style.Parse("blue"));

        var framesList = frames.ToList();
        
        if (framesList.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No stack frames available[/]");
            return;
        }

        var current = tree.AddNode($"[{GetFrameStyle(framesList[0])}]{FormatFrame(framesList[0], 0)}[/]");

        for (int i = 1; i < framesList.Count && i < 50; i++) // Limit to 50 frames
        {
            var frame = framesList[i];
            var frameText = FormatFrame(frame, i);
            var style = GetFrameStyle(frame);
            current = current.AddNode($"[{style}]{frameText}[/]");
        }

        AnsiConsole.Write(tree);
    }

    public void RenderSimplifiedTrace(IEnumerable<ClrStackFrame> frames, int maxFrames = 10)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]#[/]").Centered())
            .AddColumn(new TableColumn("[bold]Instruction Pointer[/]"))
            .AddColumn(new TableColumn("[bold]Method[/]"))
            .AddColumn(new TableColumn("[bold]Module[/]"));

        int index = 0;
        foreach (var frame in frames.Take(maxFrames))
        {
            var style = GetFrameStyle(frame);
            
            table.AddRow(
                $"[{style}]{index}[/]",
                $"[{style}]0x{frame.InstructionPointer:X}[/]",
                $"[{style}]{frame.Method?.Name ?? "Unknown"}[/]",
                $"[grey]{frame.Method?.Type?.Module?.Name ?? "N/A"}[/]"
            );

            index++;
        }

        AnsiConsole.Write(table);
    }

    private string FormatFrame(ClrStackFrame frame, int index)
    {
        var methodName = frame.Method?.Name ?? "<Unknown>";
        var typeName = frame.Method?.Type?.Name;
        var ip = $"0x{frame.InstructionPointer:X}";

        if (!string.IsNullOrEmpty(typeName))
        {
            return $"#{index} {typeName}.{methodName} @ {ip}";
        }

        return $"#{index} {methodName} @ {ip}";
    }

    private string GetFrameStyle(ClrStackFrame frame)
    {
        // Color code frames based on type
        var methodName = frame.Method?.Name?.ToLowerInvariant() ?? "";
        var typeName = frame.Method?.Type?.Name?.ToLowerInvariant() ?? "";

        // System/framework code
        if (typeName.StartsWith("system."))
            return "grey";

        // User code (likely the issue)
        if (!typeName.StartsWith("system.") && !typeName.StartsWith("microsoft."))
            return "red bold";

        // .NET runtime code
        if (typeName.Contains("runtime") || typeName.Contains("jit"))
            return "yellow";

        // Exception-related (highlight)
        if (methodName.Contains("throw") || methodName.Contains("exception"))
            return "red";

        return "white";
    }

    public Panel CreateCallPanel(string title, string content, Color borderColor)
    {
        return new Panel(content)
            .Header(title)
            .BorderColor(borderColor)
            .Padding(1, 0);
    }
}
