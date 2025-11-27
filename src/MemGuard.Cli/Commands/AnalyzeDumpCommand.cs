using Spectre.Console;
using Spectre.Console.Cli;
using MemGuard.Core;
using MemGuard.Infrastructure;
using MemGuard.AI;
using MemGuard.AI.Interface;
using MemGuard.Reporters;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Globalization;
using MemGuard.Core.Services;
using MemGuard.Core.Interfaces;
using MemGuard.Cli.Models;

namespace MemGuard.Cli.Commands;

public sealed class AnalyzeDumpCommand : Command<AnalyzeDumpSettings>
{
    public override int Execute(CommandContext context, AnalyzeDumpSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            AnsiConsole.MarkupLine("[green]MemGuard[/] - AI-powered dump analysis starting...");
            AnsiConsole.MarkupLine($"[yellow]Analyzing:[/] {settings.DumpPath}");
            AnsiConsole.MarkupLine($"[yellow]Using Model:[/] {settings.Model}");
            // Create service collection and configure dependencies
            var services = new ServiceCollection();
            ConfigureServices(services, settings);
            var serviceProvider = services.BuildServiceProvider();
            var memleaksDetectorService = serviceProvider.GetRequiredService<IMemLeakDetector>();
            memleaksDetectorService.LoadDumpFile(settings.DumpPath);
            var stopwatch = Stopwatch.StartNew();
            var analyze = new AnalysisOptions
            {
                TopN = 20,
                MaxSamplesPerType = 5
            };
            var result = memleaksDetectorService.Diagnose(analyze);
            Console.WriteLine($"Leak Report Generated at {DateTime.Now}");
            Console.WriteLine($"Root Cause: {result.RootCause}");
            Console.WriteLine($"Diagonistic Count:  {result.Diagnostics.Count}");
            AnsiConsole.WriteLine($"Leak Report Generated at {DateTime.Now}");
            AnsiConsole.WriteLine($"Root Cause: {result.RootCause}");
            AnsiConsole.WriteLine($"Diagonistic Count:  {result.Diagnostics.Count}");

            stopwatch.Stop();

            // Generate report

            // Output result
            if (!string.IsNullOrEmpty(settings.OutputPath))
            {

                AnsiConsole.MarkupLine($"[green]Report saved to:[/] {settings.OutputPath}");
            }
            else
            {
                AnsiConsole.WriteLine();

            }

            AnsiConsole.MarkupLine($"[green]Analysis completed in [/]{stopwatch.ElapsedMilliseconds}ms");
            return 0;
        }
        catch (FileNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Dump file not found: {ex.Message}");
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Access denied to dump file: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static void ConfigureServices(IServiceCollection services, AnalyzeDumpSettings settings)
    {
        // Register infrastructure services
        services.AddSingleton<TelemetryService>();
        services.AddSingleton<AnalyzeDumpSettings>(settings);
        services.AddSingleton<IMemLeakDetector, MemLeakDetectorService>();
        // Register AI services
        services.AddSingleton<ILLMClient>(provider =>
        {
            // In a real implementation, we'd configure based on settings
            return new OllamaClient(new Uri("http://localhost:11434"), settings.Model);
        });

        // Register reporters
        services.AddSingleton<IReporter>(provider =>
        {
            return settings.Format.ToUpperInvariant() switch
            {
                "MARKDOWN" => new MarkdownReporter(),
                "HTML" => new HtmlReporter(),
                "PDF" => new PdfReporter(),
                _ => new MarkdownReporter()
            };
        });

        // Register analyzers
        services.AddSingleton<IAnalyzer, HeapAnalyzer>();
        services.AddSingleton<IAnalyzer, DeadlockAnalyzer>();
        services.AddSingleton<IEnumerable<IAnalyzer>>(provider =>
            provider.GetServices<IAnalyzer>().ToList());

        // Register orchestrator
        services.AddSingleton<AnalysisOrchestrator>();
    }
}
