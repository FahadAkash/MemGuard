using Spectre.Console;
using Spectre.Console.Cli;
using MemGuard.Core;
using MemGuard.Core.Interfaces;
using MemGuard.Core.Services;
using MemGuard.Infrastructure;
using MemGuard.Infrastructure.Extractors;
using MemGuard.AI;
using MemGuard.Reporters;
using MemGuard.Cli.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace MemGuard.Cli.Commands;

public sealed class AnalyzeDumpCommand : AsyncCommand<AnalyzeDumpSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AnalyzeDumpSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            AnsiConsole.Write(new FigletText("MemGuard").Color(Color.Green));
            AnsiConsole.MarkupLine("[grey]AI-Powered .NET Diagnostic Tool[/]");
            AnsiConsole.WriteLine();

            AnsiConsole.Write(new Markup("[yellow]Analyzing:[/] "));
            AnsiConsole.WriteLine(settings.DumpPath);
            AnsiConsole.Write(new Markup("[yellow]Provider:[/] "));
            AnsiConsole.WriteLine(settings.Provider);
            AnsiConsole.Write(new Markup("[yellow]Model:[/] "));
            AnsiConsole.WriteLine(settings.Model ?? "Default");
            
            // Create service collection and configure dependencies
            var services = new ServiceCollection();
            ConfigureServices(services, settings);
            var serviceProvider = services.BuildServiceProvider();

            var orchestrator = serviceProvider.GetRequiredService<AnalysisOrchestrator>();
            var reporter = serviceProvider.GetRequiredService<IReporter>();
            var telemetry = serviceProvider.GetRequiredService<TelemetryService>();

            using var activity = telemetry.StartAnalysisActivity(settings.DumpPath);
            var stopwatch = Stopwatch.StartNew();

            AnalysisResult? result = null;
            
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Analyzing dump...", async ctx =>
                {
                    ctx.Status("Loading dump and extracting diagnostics...");
                    result = await orchestrator.AnalyzeAsync(settings.DumpPath);
                    
                    ctx.Status("Generating report...");
                });

            stopwatch.Stop();
            telemetry.RecordAnalysis(stopwatch.Elapsed.TotalSeconds, true);

            // Display summary to console
            AnsiConsole.WriteLine();
            
            // Before/After Diagnostics Comparison Table
            AnsiConsole.MarkupLine("[yellow]═══ Diagnostics Analysis ═══[/]");
            AnsiConsole.WriteLine();
            
            var diagTable = new Table();
            diagTable.AddColumn(new TableColumn("[cyan]Diagnostic Type[/]").LeftAligned());
            diagTable.AddColumn(new TableColumn("[yellow]Before (Detected)[/]").LeftAligned());
            diagTable.AddColumn(new TableColumn("[green]After (AI Analysis)[/]").LeftAligned());
            diagTable.AddColumn(new TableColumn("[red]Severity[/]").Centered());
            
            foreach (var diagnostic in result!.Diagnostics)
            {
                var severityColor = diagnostic.Severity switch
                {
                    SeverityLevel.Critical => "red",
                    SeverityLevel.Error => "orange1",
                    SeverityLevel.Warning => "yellow",
                    _ => "grey"
                };
                
                var beforeStatus = $"[grey]Issue detected[/]";
                var afterStatus = diagnostic.Type == "Heap" ? "Memory fragmentation identified" :
                                diagnostic.Type == "Deadlock" ? "Deadlock pattern analyzed" :
                                "Issue characterized by AI";
                
                diagTable.AddRow(
                    $"[cyan]{diagnostic.Type.EscapeMarkup()}[/]",
                    beforeStatus,
                    $"[green]{afterStatus.EscapeMarkup()}[/]",
                    $"[{severityColor}]{diagnostic.Severity}[/]"
                );
            }
            
            AnsiConsole.Write(diagTable);
            AnsiConsole.WriteLine();
            
            // Summary metrics
            var summaryTable = new Table();
            summaryTable.AddColumn("Metric");
            summaryTable.AddColumn("Value");
            summaryTable.AddRow("[yellow]AI Confidence[/]", $"[green]{result.ConfidenceScore:P0}[/]");
            var rootCauseDisplay = result.RootCause.Length > 70 ? result.RootCause.Substring(0, 67) + "..." : result.RootCause;
            summaryTable.AddRow("[yellow]Root Cause[/]", rootCauseDisplay.EscapeMarkup());
            summaryTable.AddRow("[yellow]Total Issues[/]", $"[red]{result.Diagnostics.Count}[/]");
            AnsiConsole.Write(summaryTable);

            // Generate report
            var outputPath = settings.OutputPath ?? Path.ChangeExtension(settings.DumpPath, ".md");
            var reportPath = await reporter.GenerateReportAsync(result, outputPath);

            // Export JSON if requested
            if (settings.ExportJson)
            {
                var jsonPath = Path.ChangeExtension(reportPath, ".json");
                var jsonData = new
                {
                    Timestamp = DateTime.Now,
                    DumpPath = settings.DumpPath,
                    Provider = settings.Provider,
                    result.RootCause,
                    result.CodeFix,
                    result.ConfidenceScore,
                    Diagnostics = result.Diagnostics.Select(d => new
                    {
                        d.Type,
                        d.Severity,
                        d.Description
                    })
                };
                var json = System.Text.Json.JsonSerializer.Serialize(jsonData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(jsonPath, json);
                AnsiConsole.Write(new Markup("[green]JSON exported to:[/] "));
                AnsiConsole.WriteLine(jsonPath);
            }

            AnsiConsole.MarkupLine($"[green]Analysis completed in {stopwatch.Elapsed.TotalSeconds:F1}s[/]");
            AnsiConsole.Write(new Markup("[green]Report saved to:[/] "));
            AnsiConsole.WriteLine(reportPath);

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            if (ex.InnerException != null)
            {
                AnsiConsole.MarkupLine($"[red]Details:[/] {ex.InnerException.Message}");
            }
            return 1;
        }
    }

    private static void ConfigureServices(IServiceCollection services, AnalyzeDumpSettings settings)
    {
        // Infrastructure
        services.AddSingleton<TelemetryService>();
        services.AddSingleton<IDumpParser, DumpParser>();
        
        // Extractors
        services.AddSingleton<IDiagnosticExtractor, HeapExtractor>();
        services.AddSingleton<IDiagnosticExtractor, DeadlockExtractor>();
        
        // AI Client
        if (settings.Provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<GeminiClient>();
            services.AddSingleton<ILLMClient>(sp => 
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GeminiClient));
                var apiKey = settings.ApiKey ?? "AIzaSyBwO0jyYU9LBexFLO2W6yU6JLovY8HgqXo"; // Default or from settings
                return new GeminiClient(httpClient, apiKey);
            });
        }
        else
        {
            services.AddSingleton<ILLMClient>(sp => 
                new OllamaClient("http://localhost:11434", settings.Model ?? "llama3.2"));
        }

        // Reporter
        services.AddSingleton<IReporter>(sp =>
        {
            return settings.Format.ToUpperInvariant() switch
            {
                "PDF" => new PdfReporter(),
                _ => new MarkdownReporter()
            };
        });

        // Orchestrator
        services.AddSingleton<AnalysisOrchestrator>();
    }
}
