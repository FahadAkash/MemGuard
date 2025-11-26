using Spectre.Console;
using Spectre.Console.Cli;
using MemGuard.Core;
using MemGuard.Infrastructure;
using MemGuard.AI;
using MemGuard.Reporters;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Globalization;

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

            // Create service collection and configure dependencies
            var services = new ServiceCollection();
            ConfigureServices(services, settings);
            var serviceProvider = services.BuildServiceProvider();

            // Get the analysis orchestrator
            var orchestrator = serviceProvider.GetRequiredService<AnalysisOrchestrator>();
            
            // Run analysis
            var stopwatch = Stopwatch.StartNew();
            var result = orchestrator.AnalyzeDump(settings.DumpPath).Result;
            stopwatch.Stop();

            // Generate report
            var reporter = serviceProvider.GetRequiredService<IReporter>();
            var report = reporter.GenerateReport(result);

            // Output result
            if (!string.IsNullOrEmpty(settings.OutputPath))
            {
                File.WriteAllText(settings.OutputPath, report);
                AnsiConsole.MarkupLine($"[green]Report saved to:[/] {settings.OutputPath}");
            }
            else
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Panel(report)
                    .Header("Analysis Results")
                    .BorderColor(Color.Green));
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

public sealed class AnalyzeDumpSettings : CommandSettings
{
    [CommandArgument(0, "<dumpPath>")]
    public string DumpPath { get; set; } = string.Empty;

    [CommandOption("-o|--output")]
    public string? OutputPath { get; set; }

    [CommandOption("--model")]
    public string Model { get; set; } = "llama3.2:13b";

    [CommandOption("-f|--format")]
    public string Format { get; set; } = "markdown";
}