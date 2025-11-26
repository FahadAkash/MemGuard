using Spectre.Console;
using Spectre.Console.Cli;

namespace MemGuard.Cli.Commands;

public sealed class AnalyzeDumpCommand : Command<AnalyzeDumpSettings>
{
    public override int Execute(CommandContext context, AnalyzeDumpSettings settings)
    {
        AnsiConsole.MarkupLine("[green]MemGuard[/] - AI-powered dump analysis starting...");
        AnsiConsole.MarkupLine($"[yellow]Analyzing:[/] {settings.DumpPath}");
        // TODO: Wire up real analysis
        AnsiConsole.MarkupLine("[green]Analysis complete![/]");
        return 0;
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
}