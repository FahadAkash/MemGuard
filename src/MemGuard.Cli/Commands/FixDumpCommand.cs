using Spectre.Console;
using Spectre.Console.Cli;
using MemGuard.Core;
using MemGuard.Core.Interfaces;
using MemGuard.Core.Models;
using MemGuard.Core.Services;
using MemGuard.Infrastructure;
using MemGuard.Infrastructure.Extractors;
using MemGuard.AI;
using MemGuard.Reporters;
using MemGuard.Cli.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace MemGuard.Cli.Commands;

public sealed class FixDumpCommand : AsyncCommand<FixDumpSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, FixDumpSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            AnsiConsole.Write(new FigletText("MemGuard Fix").Color(Color.Blue));
            AnsiConsole.MarkupLine("[grey]AI-Powered Code Fixer[/]");
            AnsiConsole.WriteLine();

            if (string.IsNullOrEmpty(settings.ProjectPath))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] --project path is required for fix command");
                return 1;
            }

            AnsiConsole.Write(new Markup("[yellow]Analyzing:[/] "));
            AnsiConsole.WriteLine(settings.DumpPath);
            AnsiConsole.Write(new Markup("[yellow]Project:[/] "));
            AnsiConsole.WriteLine(settings.ProjectPath);
            AnsiConsole.Write(new Markup("[yellow]Provider:[/] "));
            AnsiConsole.WriteLine(settings.Provider);
            AnsiConsole.Write(new Markup("[yellow]Mode:[/] "));
            AnsiConsole.WriteLine(settings.DryRun ? "Dry Run (Preview Only)" : "Apply Fixes");
            AnsiConsole.WriteLine();

            // Create service collection and configure dependencies
            var services = new ServiceCollection();
            ConfigureServices(services, settings);
            var serviceProvider = services.BuildServiceProvider();

            var dumpParser = serviceProvider.GetRequiredService<IDumpParser>();
            var extractors = serviceProvider.GetRequiredService<IEnumerable<IDiagnosticExtractor>>();
            var llmClient = serviceProvider.GetRequiredService<ILLMClient>();
            var codeFixer = serviceProvider.GetRequiredService<ICodeFixer>();
            var telemetry = serviceProvider.GetRequiredService<TelemetryService>();

            using var activity = telemetry.StartAnalysisActivity(settings.DumpPath);
            var stopwatch = Stopwatch.StartNew();

            // Step 1: Load dump and extract diagnostics
            var diagnostics = new List<DiagnosticBase>();
            
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Analyzing dump...", async ctx =>
                {
                    ctx.Status("Loading dump...");
                    using var runtime = dumpParser.LoadDump(settings.DumpPath);
                    
                    ctx.Status("Extracting diagnostics...");
                    foreach (var extractor in extractors)
                    {
                        var diagnostic = await extractor.ExtractAsync(runtime);
                        if (diagnostic != null)
                        {
                            diagnostics.Add(diagnostic);
                        }
                    }
                    
                    ctx.Status("Reading project files...");
                    var sourceFiles = new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(settings.ProjectPath) && Directory.Exists(settings.ProjectPath))
                    {
                        var files = Directory.GetFiles(settings.ProjectPath, "*.cs", SearchOption.AllDirectories)
                            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) && 
                                      !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                                      !f.Contains(Path.DirectorySeparatorChar + ".memguard" + Path.DirectorySeparatorChar))
                            .Take(20); // Limit to 20 files to avoid context limit issues

                        foreach (var file in files)
                        {
                            try
                            {
                                var content = await File.ReadAllTextAsync(file);
                                // Simple check to avoid sending huge files
                                if (content.Length < 50000) 
                                {
                                    var relativePath = Path.GetRelativePath(settings.ProjectPath, file);
                                    sourceFiles[relativePath] = content;
                                }
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Could not read {file}: {ex.Message}");
                            }
                        }
                    }
                    
                    ctx.Status("Requesting AI fixes...");
                    var prompt = PromptBuilder.BuildFixPrompt(diagnostics, settings.ProjectPath, sourceFiles);
                    var aiResponse = await llmClient.GenerateResponseAsync(prompt);
                    
                    // DEBUG LOGGING
                    AnsiConsole.MarkupLine("[yellow]DEBUG: AI Response received[/]");
                    AnsiConsole.WriteLine("----------------------------------------");
                    AnsiConsole.WriteLine(aiResponse);
                    AnsiConsole.WriteLine("----------------------------------------");

                    ctx.Status("Parsing and applying fixes...");
                    var fixResult = await codeFixer.ApplyFixesAsync(aiResponse, settings.ProjectPath!, settings.DryRun);
                    
                    stopwatch.Stop();
                    
                    // Display results
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[green]Analysis completed in {stopwatch.Elapsed.TotalSeconds:F1}s[/]");
                    AnsiConsole.WriteLine();

                    if (fixResult.AppliedFixes.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]No fixes were generated by the AI[/]");
                        return;
                    }

                    // Show fixes
                    var table = new Table();
                    table.AddColumn("File");
                    table.AddColumn("Status");
                    
                    foreach (var fix in fixResult.AppliedFixes)
                    {
                        var fileName = Path.GetFileName(fix.FilePath);
                        table.AddRow(fileName.EscapeMarkup(), settings.DryRun ? "[yellow]Preview[/]" : "[green]Applied[/]");
                    }
                    
                    AnsiConsole.Write(table);
                    AnsiConsole.WriteLine();

                    // Show summary of changes
                    var changesList = fixResult.AppliedFixes
                        .Select(f => (f.FilePath, f.LinesAdded, f.LinesRemoved, f.LinesModified))
                        .ToList();
                    
                    if (changesList.Count > 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]Changes Summary:[/]");
                        var summaryTable = DiffFormatter.CreateDiffSummaryTable(changesList);
                        AnsiConsole.Write(summaryTable);
                        AnsiConsole.WriteLine();
                    }

                    // Show colorized diffs
                    foreach (var fix in fixResult.AppliedFixes)
                    {
                        var panel = new Panel(fix.UnifiedDiff)
                        {
                            Header = new PanelHeader($"[blue]{Path.GetFileName(fix.FilePath).EscapeMarkup()}[/]"),
                            Border = BoxBorder.Rounded,
                            BorderStyle = new Style(Color.Grey)
                        };
                        AnsiConsole.Write(panel);
                        AnsiConsole.WriteLine();
                    }

                    if (!settings.DryRun && fixResult.BackupId != null)
                    {
                        AnsiConsole.MarkupLine($"[green]Backup created:[/] {fixResult.BackupId}");
                        AnsiConsole.MarkupLine($"[grey]To restore: memguard restore --backup-id {fixResult.BackupId}[/]");
                    }

                    if (fixResult.Errors.Count > 0)
                    {
                        AnsiConsole.MarkupLine("[red]Errors:[/]");
                        foreach (var error in fixResult.Errors)
                        {
                            AnsiConsole.MarkupLine($"  [red]•[/] {error.EscapeMarkup()}");
                        }
                    }

                    // Generate report if output path specified
                    if (!string.IsNullOrEmpty(settings.OutputPath))
                    {
                        await GenerateFixReportAsync(settings, diagnostics, fixResult, stopwatch);
                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine($"[green]Report saved to:[/] {settings.OutputPath}");
                    }
                });

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            if (ex.InnerException != null)
            {
                AnsiConsole.MarkupLine($"[red]Details:[/] {ex.InnerException.Message.EscapeMarkup()}");
            }
            return 1;
        }
    }

    private static void ConfigureServices(IServiceCollection services, FixDumpSettings settings)
    {
        // Infrastructure
        services.AddSingleton<TelemetryService>();
        services.AddSingleton<IDumpParser, DumpParser>();
        services.AddSingleton<IBackupManager, BackupManager>();
        services.AddSingleton<ICodeFixer, CodeFixer>();
        
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
                var apiKey = settings.ApiKey ?? "AIzaSyBwO0jyYU9LBexFLO2W6yU6JLovY8HgqXo";
                return new GeminiClient(httpClient, apiKey);
            });
        }
        else
        {
            services.AddSingleton<ILLMClient>(sp => 
                new OllamaClient("http://localhost:11434", settings.Model ?? "llama3.2"));
        }
    }

    private static async Task GenerateFixReportAsync(
        FixDumpSettings settings, 
        List<DiagnosticBase> diagnostics, 
        FixResult fixResult, 
        Stopwatch stopwatch)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("# MemGuard Fix Report");
        sb.AppendLine($"**Date:** {DateTime.Now}");
        sb.AppendLine($"**Dump File:** {Path.GetFileName(settings.DumpPath)}");
        sb.AppendLine($"**Project:** {settings.ProjectPath}");
        sb.AppendLine($"**AI Provider:** {settings.Provider}");
        sb.AppendLine($"**Mode:** {(settings.DryRun ? "Preview (Dry Run)" : "Applied")}");
        sb.AppendLine($"**Duration:** {stopwatch.Elapsed.TotalSeconds:F1}s");
        sb.AppendLine();

        // Diagnostics section
        sb.AppendLine("## Diagnostics Found");
        sb.AppendLine();
        if (diagnostics.Count > 0)
        {
            foreach (var diagnostic in diagnostics)
            {
                sb.AppendLine($"### {diagnostic.Type} ({diagnostic.Severity})");
                sb.AppendLine(diagnostic.Description);
                
                if (diagnostic is HeapDiagnostic heap)
                {
                    sb.AppendLine($"- **Fragmentation:** {heap.FragmentationLevel:P2}");
                    sb.AppendLine($"- **Total Size:** {heap.TotalSize:N0} bytes");
                }
                else if (diagnostic is DeadlockDiagnostic deadlock)
                {
                    sb.AppendLine($"- **Threads Involved:** {string.Join(", ", deadlock.ThreadIds)}");
                    sb.AppendLine($"- **Locks:**");
                    foreach (var lockObj in deadlock.LockObjects)
                    {
                        sb.AppendLine($"  - {lockObj}");
                    }
                }
                
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("*No diagnostics extracted*");
            sb.AppendLine();
        }

        // Fixes summary
        sb.AppendLine("## Fixes Applied");
        sb.AppendLine();
        if (fixResult.AppliedFixes.Count > 0)
        {
            sb.AppendLine("| File | Lines Added | Lines Removed | Lines Modified | Status |");
            sb.AppendLine("|------|-------------|----------------|----------------|--------|");
            
            foreach (var fix in fixResult.AppliedFixes)
            {
                var fileName = Path.GetFileName(fix.FilePath);
                var status = settings.DryRun ? "Preview" : "Applied";
                sb.AppendLine($"| {fileName} | {fix.LinesAdded} | {fix.LinesRemoved} | {fix.LinesModified} | {status} |");
            }
            
            sb.AppendLine();
            
            // Total statistics
            var totalAdded = fixResult.AppliedFixes.Sum(f => f.LinesAdded);
            var totalRemoved = fixResult.AppliedFixes.Sum(f => f.LinesRemoved);
            var totalModified = fixResult.AppliedFixes.Sum(f => f.LinesModified);
            
            sb.AppendLine($"**Total Changes:** +{totalAdded} -{totalRemoved} ~{totalModified}");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("*No fixes were generated*");
            sb.AppendLine();
        }

        // Detailed diffs
        if (fixResult.AppliedFixes.Count > 0)
        {
            sb.AppendLine("## Code Changes");
            sb.AppendLine();
            
            foreach (var fix in fixResult.AppliedFixes)
            {
                sb.AppendLine($"### {Path.GetFileName(fix.FilePath)}");
                sb.AppendLine();
                sb.AppendLine("```diff");
                
                // Convert colorized markup to plain diff
                var plainDiff = fix.UnifiedDiff
                    .Replace("[green]", "")
                    .Replace("[/]", "")
                    .Replace("[red]", "")
                    .Replace("[yellow]", "")
                    .Replace("[blue]", "")
                    .Replace("[cyan]", "")
                    .Replace("[grey]", "");
                
                sb.AppendLine(plainDiff);
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        // Backup information
        if (!settings.DryRun && fixResult.BackupId != null)
        {
            sb.AppendLine("## Backup Information");
            sb.AppendLine();
            sb.AppendLine($"**Backup ID:** `{fixResult.BackupId}`");
            sb.AppendLine();
            sb.AppendLine("To restore the backup:");
            sb.AppendLine("```bash");
            sb.AppendLine($"memguard restore --backup-id {fixResult.BackupId}");
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // Errors
        if (fixResult.Errors.Count > 0)
        {
            sb.AppendLine("## Errors");
            sb.AppendLine();
            foreach (var error in fixResult.Errors)
            {
                sb.AppendLine($"- {error}");
            }
            sb.AppendLine();
        }

        // Summary
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"- **Files Modified:** {fixResult.AppliedFixes.Count}");
        sb.AppendLine($"- **Success:** {(fixResult.Success ? "✓" : "✗")}");
        if (!settings.DryRun)
        {
            sb.AppendLine($"- **Backup Created:** {(fixResult.BackupId != null ? "✓" : "✗")}");
        }
        sb.AppendLine();

        var outputPath = Path.ChangeExtension(settings.OutputPath, ".md");
        await File.WriteAllTextAsync(outputPath, sb.ToString());
    }
}
