using MemGuard.Core.Agent;
using MemGuard.Core.Agent.Tools;
using MemGuard.Core.Interfaces;
using MemGuard.Infrastructure;
using MemGuard.AI;
using Spectre.Console;

namespace MemGuard.Cli.Tests;

/// <summary>
/// Simple test to verify the agent loop works
/// </summary>
public class AgentLoopTest
{
    public static async Task RunSimpleTest(ILLMClient ai)
    {
        AnsiConsole.WriteLine("Testing Agent Loop Engine...");
        AnsiConsole.WriteLine();

        // 1. Create tool registry and register tools
        var registry = new ToolRegistry();
        var fileManager = new FileManager();
        
        registry.RegisterTools(
            new ReadFileTool(fileManager),
            new ListDirectoryTool(),
            new SearchFilesTool(),
            new VerifyChangesTool()
        );

        AnsiConsole.WriteLine("✓ Registered 3 tools");
        AnsiConsole.WriteLine();

        // 2. Create agent loop
        var loop = new AgentLoop(ai, registry);
        AnsiConsole.WriteLine("✓ Created AgentLoop");
        AnsiConsole.WriteLine();

        // 3. Configure the loop
        var config = new AgentLoopConfig
        {
            MaxIterations = 5,
            MaxExecutionTime = TimeSpan.FromMinutes(1),
            Verbose = true,
            ProjectPath = Environment.CurrentDirectory,
            
            // Progress callback
            OnProgress = (state, action) =>
            {
                AnsiConsole.WriteLine($"Iteration {state.IterationCount}: Planning...");
                AnsiConsole.WriteLine($"  Thought: {action.Reasoning}");
                AnsiConsole.WriteLine($"  Tool: {action.ToolName}");
                AnsiConsole.WriteLine();
            },
            
            // Iteration complete callback
            OnIterationComplete = (state) =>
            {
                var lastAction = state.ExecutedActions.LastOrDefault();
                if (lastAction != null)
                {
                    if (lastAction.IsSuccess)
                    {
                        AnsiConsole.WriteLine($"  ✓ Success ({lastAction.ExecutionDuration?.TotalMilliseconds:F0}ms)");
                    }
                    else
                    {
                        AnsiConsole.WriteLine($"  ✗ Failed: {lastAction.Result?.Error}");
                    }
                    AnsiConsole.WriteLine();
                }
            },
            
            // Error callback
            OnError = (error) =>
            {
                AnsiConsole.WriteLine($"Error: {error}");
            }
        };

        // 4. Run a simple task
        AnsiConsole.WriteLine("Running test task: List files in current directory");
        AnsiConsole.WriteLine();

        var result = await loop.RunAsync(
            "List all C# files in the current directory", 
            config
        );

        // 5. Display results
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("Test Results");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.AddColumn("Metric");
        table.AddColumn("Value");
        
        table.AddRow("Task", Markup.Escape(result.CurrentTask));
        table.AddRow("Status", result.IsComplete ? "Complete" : "Incomplete");
        table.AddRow("Iterations", result.IterationCount.ToString());
        table.AddRow("Actions", result.ExecutedActions.Count.ToString());
        table.AddRow("Successes", result.GetSuccessfulActions().Count().ToString());
        table.AddRow("Failures", result.GetFailedActions().Count().ToString());
        table.AddRow("Errors", result.Errors.Count.ToString());
        table.AddRow("Duration", $"{result.ElapsedTime.TotalSeconds:F1}s");
        
        if (result.CompletionReason != null)
        {
            table.AddRow("Completion", Markup.Escape(result.CompletionReason));
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Show action history
        if (result.ExecutedActions.Any())
        {
            AnsiConsole.WriteLine("Action History:");
            foreach (var action in result.ExecutedActions)
            {
                var status = action.IsSuccess ? "✓" : "✗";
                AnsiConsole.WriteLine($"  {status} {action.ToolName}");
            }
            AnsiConsole.WriteLine();
        }

        // Show final state summary
        AnsiConsole.WriteLine("Final State:");
        AnsiConsole.WriteLine(result.GetSummary());
        
        return;
    }
}
