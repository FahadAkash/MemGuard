using Spectre.Console;
using Spectre.Console.Cli;
using MemGuard.Cli.Commands;

#pragma warning disable CA1861, CA2007

// Display Branding
AnsiConsole.Write(
    new FigletText("MemGuard")
        .LeftJustified()
        .Color(Color.Cyan1));
AnsiConsole.MarkupLine("[bold yellow]AI-Powered .NET Memory Diagnostic Tool[/]");
AnsiConsole.MarkupLine("[grey]Made by Fahad Akash[/]");
AnsiConsole.WriteLine();

var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("memguard");
    
    // Analyze command
    config.AddCommand<AnalyzeDumpCommand>("analyze")
        .WithDescription("Analyze a memory dump and generate a report")
        .WithExample(new[] { "analyze", "crash.dmp", "--provider", "Gemini" });
    
    // Fix command
    config.AddCommand<FixDumpCommand>("fix")
        .WithDescription("Analyze a dump and apply AI-suggested fixes to code")
        .WithExample(new[] { "fix", "crash.dmp", "--project", "./MyApp", "--dry-run" });
    
    // Restore command
    config.AddCommand<RestoreCommand>("restore")
        .WithDescription("Restore files from a backup")
        .WithExample(new[] { "restore", "--list" })
        .WithExample(new[] { "restore", "--latest" });
    
    // Monitor command
    config.AddCommand<MonitorCommand>("monitor")
        .WithDescription("Monitor a live process for memory issues")
        .WithExample(new[] { "monitor", "--process", "MyApp", "--interval", "5" })
        .WithExample(new[] { "monitor", "--pid", "1234", "--alert-threshold", "500" });
    
    // Compare command
    config.AddCommand<CompareCommand>("compare")
        .WithDescription("Compare two memory dumps")
        .WithExample(new[] { "compare", "before.dmp", "after.dmp", "--output", "diff.md" });
    
    // Agent command
    config.AddCommand<AgentCommand>("agent")
        .WithDescription("Interactive AI agent for project assistance")
        .WithExample(new[] { "agent", "--project", "./MyApp", "--provider", "claude" })
        .WithExample(new[] { "agent", "--autonomous" });
});

return await app.RunAsync(args).ConfigureAwait(false);