using Spectre.Console;
using Spectre.Console.Cli;
using MemGuard.Cli.Commands;
using System.Linq;

#pragma warning disable CA1861, CA2007

// Display Branding
AnsiConsole.Write(
    new FigletText("MemGuard")
        .LeftJustified()
        .Color(Color.Cyan1));
AnsiConsole.MarkupLine("[bold yellow]AI-Powered .NET Memory Diagnostic Tool[/]");
AnsiConsole.MarkupLine("[grey]Made by Fahad Akash[/]");
AnsiConsole.WriteLine();

// Handle --example argument
if (args.Contains("--example") || args.Contains("-e"))
{
    ShowExamples();
    return 0;
}

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

static void ShowExamples()
{
    var table = new Table();
    table.Border(TableBorder.Rounded);
    table.Title("[bold yellow]MemGuard Command Examples[/]");
    table.AddColumn(new TableColumn("[cyan]Command[/]").Centered());
    table.AddColumn(new TableColumn("[green]Description[/]"));
    table.AddColumn(new TableColumn("[magenta]Example Usage[/]"));

    table.AddRow("analyze", "Analyze a memory dump", "memguard analyze crash.dmp --provider Gemini");
    table.AddRow("fix", "Auto-fix code from dump", "memguard fix crash.dmp --project . --dry-run");
    table.AddRow("monitor", "Monitor live process", "memguard monitor --process MyApp --interval 5");
    table.AddRow("agent", "Interactive AI agent", "memguard agent --project . --provider Claude");
    table.AddRow("restore", "Restore from backup", "memguard restore --list");
    table.AddRow("compare", "Compare two dumps", "memguard compare before.dmp after.dmp");

    AnsiConsole.Write(table);

    var optionsTable = new Table();
    optionsTable.Border(TableBorder.Rounded);
    optionsTable.Title("[bold yellow]Common Options[/]");
    optionsTable.AddColumn(new TableColumn("[cyan]Option[/]").Centered());
    optionsTable.AddColumn(new TableColumn("[green]Description[/]"));

    optionsTable.AddRow("--provider", "AI Provider (Gemini, Claude, Grok, DeepSeek, Ollama)");
    optionsTable.AddRow("--api-key", "API Key for the AI provider");
    optionsTable.AddRow("--project", "Path to the target project");
    optionsTable.AddRow("--dry-run", "Preview changes without applying them");
    optionsTable.AddRow("--output", "Path to save output report");
    optionsTable.AddRow("--process", "Name of process to monitor");

    AnsiConsole.Write(optionsTable);

    AnsiConsole.MarkupLine("\n[grey]Run 'memguard <command> --help' for more details.[/]");
}