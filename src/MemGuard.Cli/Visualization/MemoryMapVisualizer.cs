using Spectre.Console;

namespace MemGuard.Cli.Visualization;

/// <summary>
/// Visualizes memory layout and usage with charts and maps
/// </summary>
public class MemoryMapVisualizer
{
    public void RenderMemoryOverview(long totalMemory, long usedMemory, long freeMemory, double fragmentation)
    {
        var panel = new Panel(
            GenerateMemoryBarChart(totalMemory, usedMemory, freeMemory, fragmentation))
            .Header("[bold green]Memory Overview[/]")
            .BorderColor(Color.Green)
            .Padding(1, 0);

        AnsiConsole.Write(panel);
    }

    private string GenerateMemoryBarChart(long total, long used, long free, double fragmentation)
    {
        var usedPercent = (double)used / total;
        var freePercent = (double)free / total;

        var barWidth = 50;
        var usedBlocks = (int)(usedPercent * barWidth);
        var freeBlocks = barWidth - usedBlocks;

        var bar = new System.Text.StringBuilder();
        
        bar.AppendLine($"Total Memory: {FormatBytes(total)}");
        bar.AppendLine($"Used:  {FormatBytes(used)} ({usedPercent:P1})");
        bar.AppendLine($"Free:  {FormatBytes(free)} ({freePercent:P1})");
        bar.AppendLine();
        bar.AppendLine("Memory Usage:");
        bar.Append("[green]");
        bar.Append(new string('█', usedBlocks));
        bar.Append("[/][grey]");
        bar.Append(new string('░', freeBlocks));
        bar.AppendLine("[/]");
        bar.AppendLine();
        bar.AppendLine($"Fragmentation: {fragmentation:P2} {GetFragmentationIndicator(fragmentation)}");

        return bar.ToString();
    }

    public void RenderHeapSegments(List<(ulong Start, ulong End, long Size, string Type)> segments)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Yellow)
            .AddColumn("[bold]Start Address[/]")
            .AddColumn("[bold]End Address[/]")
            .AddColumn("[bold]Size[/]")
            .AddColumn("[bold]Type[/]")
            .AddColumn("[bold]Visual[/]");

        foreach (var segment in segments.Take(15))
        {
            var sizeBar = GenerateSizeBar(segment.Size, segments.Max(s => s.Size));
            var typeColor = segment.Type.Contains("Large") ? "yellow" : "cyan";

            table.AddRow(
                $"[grey]0x{segment.Start:X}[/]",
                $"[grey]0x{segment.End:X}[/]",
                $"[white]{FormatBytes(segment.Size)}[/]",
                $"[{typeColor}]{segment.Type}[/]",
                sizeBar
            );
        }

        AnsiConsole.Write(table);
    }

    public void RenderTypeDistribution(List<(string TypeName, long Size, int Count)> topTypes)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Top Memory Consumers[/]").RuleStyle(Style.Parse("blue")));
        AnsiConsole.WriteLine();

        var chart = new BarChart()
            .Width(60)
            .Label("[bold underline]Memory by Type[/]");

        foreach (var (typeName, size, count) in topTypes.Take(10))
        {
            var shortName = typeName.Length > 30 ? typeName.Substring(0, 27) + "..." : typeName;
            var sizeInMB = size / (1024.0 * 1024.0);
            
            chart.AddItem(shortName, sizeInMB, GetColorForSize(sizeInMB));
        }

        AnsiConsole.Write(chart);

        // Also show as table with details
        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("[bold]Type[/]")
            .AddColumn("[bold]Instances[/]")
            .AddColumn("[bold]Total Size[/]");

        foreach (var (typeName, size, count) in topTypes.Take(10))
        {
            table.AddRow(
                Markup.Escape(typeName),
                $"[cyan]{count:N0}[/]",
                $"[yellow]{FormatBytes(size)}[/]"
            );
        }

        AnsiConsole.Write(table);
    }

    public void RenderFragmentationHeatMap(double fragmentation)
    {
        var heatMap = new Panel(GenerateFragmentationArt(fragmentation))
            .Header($"[bold]Fragmentation Level: {fragmentation:P2}[/]")
            .BorderColor(GetFragmentationColor(fragmentation))
            .Padding(1, 1);

        AnsiConsole.Write(heatMap);
    }

    private string GenerateFragmentationArt(double fragmentation)
    {
        var art = new System.Text.StringBuilder();
        var blocks = 20;
        var fragmentedBlocks = (int)(fragmentation * blocks);

        art.AppendLine("Memory Fragmentation Visualization:");
        art.AppendLine();
        art.Append("│");

        for (int i = 0; i < blocks; i++)
        {
            if (i < fragmentedBlocks)
            {
                art.Append("[red]█[/]");
            }
            else
            {
                art.Append("[green]█[/]");
            }
        }
        art.AppendLine("│");
        art.AppendLine();
        art.AppendLine($"[red]█[/] Fragmented ({fragmentation:P0})  [green]█[/] Contiguous ({(1 - fragmentation):P0})");

        return art.ToString();
    }

    private string GenerateSizeBar(long size, long maxSize)
    {
        var barWidth = 20;
        var percent = (double)size / maxSize;
        var blocks = Math.Max(1, (int)(percent * barWidth));

        var color = percent > 0.7 ? "red" : percent > 0.4 ? "yellow" : "green";
        return $"[{color}]{new string('█', blocks)}[/]";
    }

    private Color GetColorForSize(double sizeMB)
    {
        if (sizeMB > 100) return Color.Red;
        if (sizeMB > 50) return Color.Orange1;
        if (sizeMB > 10) return Color.Yellow;
        return Color.Green;
    }

    private Color GetFragmentationColor(double fragmentation)
    {
        if (fragmentation > 0.5) return Color.Red;
        if (fragmentation > 0.3) return Color.Yellow;
        return Color.Green;
    }

    private string GetFragmentationIndicator(double fragmentation)
    {
        if (fragmentation > 0.5) return "[red]⚠ High[/]";
        if (fragmentation > 0.3) return "[yellow]⚠ Moderate[/]";
        return "[green]✓ Low[/]";
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
