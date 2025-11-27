using Spectre.Console;
using Spectre.Console.Cli;
using MemGuard.Core.Interfaces;
using MemGuard.Infrastructure;
using MemGuard.Infrastructure.Extractors;
using MemGuard.Cli.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace MemGuard.Cli.Commands;

public sealed class CompareCommand : AsyncCommand<CompareSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CompareSettings settings)
    {
        try
        {
            AnsiConsole.Write(new FigletText("MemGuard Compare").Color(Color.Yellow));
            AnsiConsole.MarkupLine("[grey]Dump Comparison Analysis[/]");
            AnsiConsole.WriteLine();

            var services = new ServiceCollection();
            services.AddSingleton<IDumpParser, DumpParser>();
            services.AddSingleton<IDiagnosticExtractor, HeapExtractor>();
            services.AddSingleton<IDiagnosticExtractor, DeadlockExtractor>();
            var serviceProvider = services.BuildServiceProvider();

            var dumpParser = serviceProvider.GetRequiredService<IDumpParser>();
            var extractors = serviceProvider.GetRequiredService<IEnumerable<IDiagnosticExtractor>>();

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Analyzing dumps...", async ctx =>
                {
                    ctx.Status("Loading first dump...");
                    var beforeDiagnostics = new List<Core.DiagnosticBase>();
                    using (var beforeRuntime = dumpParser.LoadDump(settings.BeforeDump))
                    {
                        foreach (var extractor in extractors)
                        {
                            var diagnostic = await extractor.ExtractAsync(beforeRuntime);
                            if (diagnostic != null) beforeDiagnostics.Add(diagnostic);
                        }
                    }

                    ctx.Status("Loading second dump...");
                    var afterDiagnostics = new List<Core.DiagnosticBase>();
                    using (var afterRuntime = dumpParser.LoadDump(settings.AfterDump))
                    {
                        foreach (var extractor in extractors)
                        {
                            var diagnostic = await extractor.ExtractAsync(afterRuntime);
                            if (diagnostic != null) afterDiagnostics.Add(diagnostic);
                        }
                    }

                    ctx.Status("Generating comparison...");

                    // Display comparison
                    AnsiConsole.WriteLine();
                    var table = new Table();
                    table.Border(TableBorder.Rounded);
                    table.AddColumn("Metric");
                    table.AddColumn("Before");
                    table.AddColumn("After");
                    table.AddColumn("Change");

                    // Compare heap
                    var beforeHeap = beforeDiagnostics.OfType<Core.HeapDiagnostic>().FirstOrDefault();
                    var afterHeap = afterDiagnostics.OfType<Core.HeapDiagnostic>().FirstOrDefault();

                    if (beforeHeap != null && afterHeap != null)
                    {
                        var sizeDelta = afterHeap.TotalSize - beforeHeap.TotalSize;
                        var fragDelta = afterHeap.FragmentationLevel - beforeHeap.FragmentationLevel;

                        table.AddRow(
                            "Heap Size",
                            $"{beforeHeap.TotalSize:N0} bytes",
                            $"{afterHeap.TotalSize:N0} bytes",
                            FormatDelta(sizeDelta, "bytes"));

                        table.AddRow(
                            "Fragmentation",
                            $"{beforeHeap.FragmentationLevel:P2}",
                            $"{afterHeap.FragmentationLevel:P2}",
                            FormatDelta(fragDelta * 100, "%"));
                    }

                    // Compare deadlocks
                    var beforeDeadlock = beforeDiagnostics.OfType<Core.DeadlockDiagnostic>().FirstOrDefault();
                    var afterDeadlock = afterDiagnostics.OfType<Core.DeadlockDiagnostic>().FirstOrDefault();

                    var beforeThreads = beforeDeadlock?.ThreadIds.Count ?? 0;
                    var afterThreads = afterDeadlock?.ThreadIds.Count ?? 0;

                    table.AddRow(
                        "Locked Threads",
                        beforeThreads.ToString(),
                        afterThreads.ToString(),
                        FormatDelta(afterThreads - beforeThreads, ""));

                    AnsiConsole.Write(table);

                    // Generate report
                    if (!string.IsNullOrEmpty(settings.OutputPath))
                    {
                        var report = GenerateReport(beforeDiagnostics, afterDiagnostics, settings.BeforeDump, settings.AfterDump);
                        await File.WriteAllTextAsync(settings.OutputPath, report);
                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine($"[green]Report saved to:[/] {settings.OutputPath}");
                    }
                });

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }
    }

    private static string FormatDelta(double delta, string unit)
    {
        var sign = delta >= 0 ? "+" : "";
        var color = delta > 0 ? "red" : delta < 0 ? "green" : "grey";
        return $"[{color}]{sign}{delta:N0}{unit}[/]";
    }

    private static string GenerateReport(List<Core.DiagnosticBase> before, List<Core.DiagnosticBase> after, string beforePath, string afterPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Memory Dump Comparison Report");
        sb.AppendLine($"**Date:** {DateTime.Now}");
        sb.AppendLine();
        sb.AppendLine($"**Before:** {Path.GetFileName(beforePath)}");
        sb.AppendLine($"**After:** {Path.GetFileName(afterPath)}");
        sb.AppendLine();

        sb.AppendLine("## Heap Comparison");
        var beforeHeap = before.OfType<Core.HeapDiagnostic>().FirstOrDefault();
        var afterHeap = after.OfType<Core.HeapDiagnostic>().FirstOrDefault();

        if (beforeHeap != null && afterHeap != null)
        {
            var sizeDelta = afterHeap.TotalSize - beforeHeap.TotalSize;
            sb.AppendLine($"- **Size Change:** {sizeDelta:N0} bytes ({(sizeDelta > 0 ? "+" : "")}{sizeDelta * 100.0 / beforeHeap.TotalSize:F2}%)");
            sb.AppendLine($"- **Fragmentation Change:** {(afterHeap.FragmentationLevel - beforeHeap.FragmentationLevel) * 100:F2}%");
        }

        sb.AppendLine();
        sb.AppendLine("## Deadlock Comparison");
        var beforeDeadlock = before.OfType<Core.DeadlockDiagnostic>().FirstOrDefault();
        var afterDeadlock = after.OfType<Core.DeadlockDiagnostic>().FirstOrDefault();

        var beforeThreads = beforeDeadlock?.ThreadIds.Count ?? 0;
        var afterThreads = afterDeadlock?.ThreadIds.Count ?? 0;

        sb.AppendLine($"- **Locked Threads Before:** {beforeThreads}");
        sb.AppendLine($"- **Locked Threads After:** {afterThreads}");
        sb.AppendLine($"- **Change:** {afterThreads - beforeThreads}");

        return sb.ToString();
    }
}
