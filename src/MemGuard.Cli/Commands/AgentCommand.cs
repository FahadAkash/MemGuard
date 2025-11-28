using Spectre.Console;
using Spectre.Console.Cli;
using MemGuard.AI;
using MemGuard.Cli.Models;
using MemGuard.Core.Interfaces;
using MemGuard.Core.Agent;
using MemGuard.Core.Agent.Tools;
using MemGuard.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MemGuard.Cli.Commands;

public sealed class AgentCommand : AsyncCommand<AgentSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AgentSettings settings)
    {
        try
        {
            // Display welcome banner
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new FigletText("MemGuard AI").Color(Color.Cyan1));
            AnsiConsole.MarkupLine("[grey]Autonomous .NET Developer Agent[/]");
            AnsiConsole.WriteLine();

            // Setup services
            var services = new ServiceCollection();
            services.AddHttpClient();
            services.AddSingleton<IFileManager, FileManager>();
            services.AddSingleton<IDumpParser, DumpParser>();
            var serviceProvider = services.BuildServiceProvider();
            
            var factory = new LLMProviderFactory(serviceProvider.GetRequiredService<IHttpClientFactory>());
            var fileManager = serviceProvider.GetRequiredService<IFileManager>();
            var dumpParser = serviceProvider.GetRequiredService<IDumpParser>();
            
            // Get API key
            var apiKey = settings.ApiKey ?? GetApiKeyFromEnvironment(settings.Provider);
            if (string.IsNullOrEmpty(apiKey) && settings.Provider != "ollama")
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] No API key provided for {settings.Provider}");
                AnsiConsole.MarkupLine($"[yellow]Set environment variable:[/] MEMGUARD_{settings.Provider.ToUpper()}_KEY");
                AnsiConsole.MarkupLine($"[yellow]Or use:[/] --api-key YOUR_KEY");
                return 1;
            }

            var ai = factory.CreateClient(settings.Provider, apiKey ?? "", settings.Model);

            // Run test mode if requested
            if (settings.Test)
            {
                AnsiConsole.MarkupLine("[yellow]Running in TEST mode[/]");
                AnsiConsole.WriteLine();
                await MemGuard.Cli.Tests.AgentLoopTest.RunSimpleTest(ai);
                return 0;
            }

            // Initialize Tool Registry
            var registry = new ToolRegistry();
            registry.RegisterTools(
                new ReadFileTool(fileManager),
                new WriteFileTool(fileManager), // Enabled writing!
                new ListDirectoryTool(),
                new SearchFilesTool(),
                new AnalyzeProjectTool(fileManager),
                new AnalyzeDumpTool(dumpParser),
                new RunCommandTool(),
                new VerifyChangesTool(),
                new KillProcessTool() // NEW: Process management
            );

            // Display configuration
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("Setting");
            table.AddColumn("Value");
            table.AddRow("AI Provider", settings.Provider);
            table.AddRow("Model", settings.Model ?? LLMProviderFactory.GetDefaultModel(settings.Provider));
            table.AddRow("Project", settings.ProjectPath ?? Environment.CurrentDirectory);
            table.AddRow("Mode", settings.Autonomous ? "[red]Autonomous[/]" : "[green]Interactive[/]");
            table.AddRow("Tools", registry.GetAllTools().Count.ToString());
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            // Main conversation loop
            while (true)
            {
                // Get user input
                var userInput = AnsiConsole.Ask<string>("[cyan]Task >[/]");
                
                if (string.IsNullOrWhiteSpace(userInput)) continue;
                if (IsExitCommand(userInput)) break;

                // Configure Agent Loop
                var config = new AgentLoopConfig
                {
                    MaxIterations = settings.MaxTurns,
                    MaxExecutionTime = TimeSpan.FromMinutes(10),
                    Verbose = true,
                    ProjectPath = settings.ProjectPath ?? Environment.CurrentDirectory,
                    AutoSaveCheckpoints = true,
                    RequireConfirmation = !settings.Autonomous,
                    
                    // UI Callbacks
                    OnProgress = (state, action) => DisplayProgress(state, action),
                    OnIterationComplete = (state) => DisplayIterationResult(state),
                    OnError = (error) => AnsiConsole.MarkupLine($"[red]Error:[/] {error.EscapeMarkup()}")
                };

                // Create and Run Agent
                var loop = new AgentLoop(ai, registry);
                
                AnsiConsole.MarkupLine($"[grey]Starting agent loop for task:[/] {userInput.EscapeMarkup()}");
                AnsiConsole.WriteLine();

                // Create cancellation token source for graceful shutdown
                using var cts = new CancellationTokenSource();
                
                // Handle Ctrl+C
                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true; // Prevent immediate termination
                    cts.Cancel();
                    AnsiConsole.MarkupLine("[yellow]Cancellation requested. Stopping agent...[/]");
                };

                try 
                {
                    var result = await loop.RunAsync(userInput, config, cts.Token);
                    DisplayFinalResult(result);
                }
                catch (OperationCanceledException)
                {
                    AnsiConsole.MarkupLine("[yellow]Agent execution cancelled by user.[/]");
                }
            }

            AnsiConsole.MarkupLine("[green]Goodbye! 👋[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Fatal Error:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }
    }

    private static void DisplayProgress(AgentState state, AgentAction action)
    {
        var rule = new Rule($"[yellow]Iteration {state.IterationCount}[/]");
        rule.Style = Style.Parse("grey");
        AnsiConsole.Write(rule);

        AnsiConsole.MarkupLine($"[bold]Thinking...[/]");
        AnsiConsole.MarkupLine($"[grey]{action.Reasoning.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine($"[bold cyan]Action:[/] {action.ToolName}");
        // Show parameters if not too long
        if (action.Parameters.Length < 200)
        {
            AnsiConsole.MarkupLine($"[grey]Params: {action.Parameters.EscapeMarkup()}[/]");
        }
    }

    private static void DisplayIterationResult(AgentState state)
    {
        var lastAction = state.ExecutedActions.LastOrDefault();
        if (lastAction != null)
        {
            if (lastAction.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[green]✓ Success[/] ({lastAction.ExecutionDuration?.TotalMilliseconds:F0}ms)");
                // Show output preview
                if (!string.IsNullOrEmpty(lastAction.Result?.Output))
                {
                    var preview = lastAction.Result.Output.Length > 300 
                        ? lastAction.Result.Output.Substring(0, 300) + "..." 
                        : lastAction.Result.Output;
                    AnsiConsole.MarkupLine($"[grey]Output: {preview.EscapeMarkup()}[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗ Failed[/]: {lastAction.Result?.Error?.EscapeMarkup()}");
            }
        }
        AnsiConsole.WriteLine();
    }

    private static void DisplayFinalResult(AgentState state)
    {
        AnsiConsole.Write(new Rule("[green]Task Complete[/]"));
        
        if (state.CompletionReason != null)
        {
            AnsiConsole.MarkupLine($"[bold]Result:[/] {state.CompletionReason.EscapeMarkup()}");
        }

        var table = new Table();
        table.AddColumn("Metric");
        table.AddColumn("Value");
        table.AddRow("Iterations", state.IterationCount.ToString());
        table.AddRow("Actions", state.ExecutedActions.Count.ToString());
        table.AddRow("Duration", $"{state.ElapsedTime.TotalSeconds:F1}s");
        table.AddRow("Cost (Est.)", "$0.00"); // TODO: Implement cost tracking
        
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static bool IsExitCommand(string input)
    {
        return input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
               input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
               input.Equals("bye", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetApiKeyFromEnvironment(string provider)
    {
        var envVar = $"MEMGUARD_{provider.ToUpper()}_KEY";
        return Environment.GetEnvironmentVariable(envVar);
    }
}
