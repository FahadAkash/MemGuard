using Spectre.Console;
using Spectre.Console.Cli;
using MemGuard.Core.Interfaces;
using MemGuard.Infrastructure;
using MemGuard.Infrastructure.Extractors;
using MemGuard.Cli.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text.Json;

namespace MemGuard.Cli.Commands;

public sealed class MonitorCommand : AsyncCommand<MonitorSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, MonitorSettings settings)
    {
        try
        {
            AnsiConsole.Write(new FigletText("MemGuard Monitor").Color(Color.Aqua));
            AnsiConsole.MarkupLine("[grey]Live Process Monitoring[/]");
            AnsiConsole.WriteLine();

            // Get process
            Process? process = null;
            if (settings.ProcessId.HasValue)
            {
                process = Process.GetProcessById(settings.ProcessId.Value);
            }
            else if (!string.IsNullOrEmpty(settings.ProcessName))
            {
                var processes = Process.GetProcessesByName(settings.ProcessName);
                if (processes.Length == 0)
                {
                    AnsiConsole.MarkupLine($"[red]Process '{settings.ProcessName}' not found[/]");
                    return 1;
                }
                process = processes[0];
                if (processes.Length > 1)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning: Multiple processes found, using PID {process.Id}[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Error: Specify --process or --pid[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[green]Monitoring:[/] {process.ProcessName} (PID: {process.Id})");
            AnsiConsole.MarkupLine($"[green]Interval:[/] {settings.IntervalSeconds}s");
            if (settings.AlertThresholdMB.HasValue)
            {
                AnsiConsole.MarkupLine($"[green]Alert Threshold:[/] {settings.AlertThresholdMB}MB");
            }
            AnsiConsole.WriteLine();

            var samples = new List<MonitorSample>();
            var startTime = DateTime.Now;
            var endTime = settings.DurationSeconds > 0 
                ? startTime.AddSeconds(settings.DurationSeconds) 
                : DateTime.MaxValue;

            // Create live chart
            await AnsiConsole.Live(CreateTable(samples))
                .StartAsync(async ctx =>
                {
                    while (DateTime.Now < endTime)
                    {
                        try
                        {
                            process.Refresh();
                            
                            var sample = new MonitorSample
                            {
                                Timestamp = DateTime.Now,
                                WorkingSetMB = process.WorkingSet64 / 1024.0 / 1024.0,
                                PrivateMemoryMB = process.PrivateMemorySize64 / 1024.0 / 1024.0,
                                VirtualMemoryMB = process.VirtualMemorySize64 / 1024.0 / 1024.0,
                                ThreadCount = process.Threads.Count,
                                HandleCount = process.HandleCount
                            };

                            samples.Add(sample);

                            // Check alert threshold
                            if (settings.AlertThresholdMB.HasValue && 
                                sample.WorkingSetMB > settings.AlertThresholdMB.Value)
                            {
                                AnsiConsole.MarkupLine($"[red]⚠ ALERT: Memory exceeded {settings.AlertThresholdMB}MB! Current: {sample.WorkingSetMB:F2}MB[/]");
                            }

                            // Update display
                            ctx.UpdateTarget(CreateTable(samples));

                            await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds));
                        }
                        catch (InvalidOperationException)
                        {
                            AnsiConsole.MarkupLine("[yellow]Process has exited[/]");
                            break;
                        }
                    }
                });

            // Save to file if requested
            if (!string.IsNullOrEmpty(settings.OutputPath))
            {
                var json = JsonSerializer.Serialize(samples, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(settings.OutputPath, json);
                AnsiConsole.MarkupLine($"[green]Data saved to:[/] {settings.OutputPath}");
            }

            // Summary
            if (samples.Count > 0)
            {
                AnsiConsole.WriteLine();
                var summaryTable = new Table();
                summaryTable.AddColumn("Metric");
                summaryTable.AddColumn("Min");
                summaryTable.AddColumn("Max");
                summaryTable.AddColumn("Avg");
                
                summaryTable.AddRow(
                    "Working Set (MB)",
                    $"{samples.Min(s => s.WorkingSetMB):F2}",
                    $"{samples.Max(s => s.WorkingSetMB):F2}",
                    $"{samples.Average(s => s.WorkingSetMB):F2}");
                
                summaryTable.AddRow(
                    "Threads",
                    $"{samples.Min(s => s.ThreadCount)}",
                    $"{samples.Max(s => s.ThreadCount)}",
                    $"{samples.Average(s => s.ThreadCount):F0}");

                AnsiConsole.Write(summaryTable);
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }
    }

    private static Table CreateTable(List<MonitorSample> samples)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Time");
        table.AddColumn("Working Set");
        table.AddColumn("Private");
        table.AddColumn("Threads");
        table.AddColumn("Handles");

        // Show last 10 samples
        foreach (var sample in samples.TakeLast(10))
        {
            table.AddRow(
                sample.Timestamp.ToString("HH:mm:ss"),
                $"{sample.WorkingSetMB:F2} MB",
                $"{sample.PrivateMemoryMB:F2} MB",
                sample.ThreadCount.ToString(),
                sample.HandleCount.ToString());
        }

        return table;
    }

    private class MonitorSample
    {
        public DateTime Timestamp { get; set; }
        public double WorkingSetMB { get; set; }
        public double PrivateMemoryMB { get; set; }
        public double VirtualMemoryMB { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
    }
}
