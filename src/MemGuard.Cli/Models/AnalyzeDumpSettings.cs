using Spectre.Console.Cli;

namespace MemGuard.Cli.Models;


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
 
