using Spectre.Console;
using Spectre.Console.Rendering;

namespace MemGuard.Cli.Visualization;

/// <summary>
/// Renders call graphs for deadlocks and complex execution flows
/// </summary>
public class CallGraphRenderer
{
    public void RenderDeadlockGraph(List<(int ThreadId, List<string> WaitingOn)> deadlockInfo)
    {
        if (deadlockInfo.Count == 0)
            return;

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[red bold]Deadlock Cycle Detected[/]")
            .RuleStyle(Style.Parse("red")));
        AnsiConsole.WriteLine();

        // Create a visual representation of the deadlock
        var canvas = new Canvas(80, Math.Min(20, deadlockInfo.Count * 4));
        
        // For simpler rendering, use Spectre.Console's Tree
        var tree = new Tree("[red]Deadlock Chain[/]");

        for (int i = 0; i < deadlockInfo.Count; i++)
        {
            var (threadId, waitingOn) = deadlockInfo[i];
            var nextThread = deadlockInfo[(i + 1) % deadlockInfo.Count].ThreadId;

            var node = tree.AddNode($"[yellow]Thread {threadId}[/]");
            
            foreach (var lockObj in waitingOn.Take(3))
            {
                node.AddNode($"[grey]Waiting on: {lockObj}[/]");
            }

            if (nextThread != threadId)
            {
                node.AddNode($"[red]→ Blocks Thread {nextThread}[/]");
            }
        }

        AnsiConsole.Write(tree);

        AnsiConsole.WriteLine();
        RenderDeadlockDiagram(deadlockInfo);
    }

    private void RenderDeadlockDiagram(List<(int ThreadId, List<string> WaitingOn)> deadlockInfo)
    {
        // ASCII art diagram
        var panel = new Panel(GenerateDeadlockAsciiArt(deadlockInfo))
            .Header("[red]Deadlock Visualization[/]")
            .BorderColor(Color.Red)
            .Padding(2, 1);

        AnsiConsole.Write(panel);
    }

    private string GenerateDeadlockAsciiArt(List<(int ThreadId, List<string> WaitingOn)> deadlockInfo)
    {
        var art = new System.Text.StringBuilder();
        
        art.AppendLine("Deadlock Cycle:");
        art.AppendLine();

        for (int i = 0; i < deadlockInfo.Count; i++)
        {
            var (threadId, waitingOn) = deadlockInfo[i];
            var nextIdx = (i + 1) % deadlockInfo.Count;
            var nextThreadId = deadlockInfo[nextIdx].ThreadId;

            art.AppendLine($"  ┌────────────────┐");
            art.AppendLine($"  │ Thread {threadId,-7} │");
            art.AppendLine($"  └────────────────┘");
            art.AppendLine($"         │");
            art.AppendLine($"         │ holds lock on");
            art.AppendLine($"         │ {(waitingOn.FirstOrDefault() ?? "Unknown")}");
            art.AppendLine($"         ↓");
            
            if (i < deadlockInfo.Count - 1)
            {
                art.AppendLine($"    (waits for)");
                art.AppendLine();
            }
            else
            {
                art.AppendLine($"    (waits for Thread {deadlockInfo[0].ThreadId})");
                art.AppendLine($"         ↑");
                art.AppendLine($"         └─────── [CYCLE]");
            }
        }

        return art.ToString();
    }

    public void RenderCallFlow(List<string> callSequence)
    {
        var tree = new Tree("[bold]Call Flow[/]");
        var current = tree.AddNode($"[cyan]{callSequence.FirstOrDefault() ?? "Unknown"}[/]");

        foreach (var call in callSequence.Skip(1).Take(19))
        {
            current = current.AddNode($"[white]{call}[/]");
        }

        AnsiConsole.Write(tree);
    }

    public void RenderThreadInteractions(Dictionary<int, List<string>> threadActivities)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .AddColumn("[bold]Thread ID[/]")
            .AddColumn("[bold]Activity[/]")
            .AddColumn("[bold]Status[/]");

        foreach (var (threadId, activities) in threadActivities.Take(10))
        {
            var activitySummary = activities.FirstOrDefault() ?? "Idle";
            var status = activities.Count > 1 ? "[yellow]Active[/]" : "[grey]Waiting[/]";

            table.AddRow(
                $"[cyan]{threadId}[/]",
                activitySummary,
                status
            );
        }

        AnsiConsole.Write(table);
    }
}
