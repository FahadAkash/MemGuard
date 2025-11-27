using Spectre.Console.Cli;
using System.ComponentModel;

namespace MemGuard.Cli.Models;

public sealed class CompareSettings : CommandSettings
{
    [CommandArgument(0, "<beforeDump>")]
    [Description("First dump file (before)")]
    public string BeforeDump { get; set; } = string.Empty;

    [CommandArgument(1, "<afterDump>")]
    [Description("Second dump file (after)")]
    public string AfterDump { get; set; } = string.Empty;

    [CommandOption("-o|--output")]
    [Description("Output file for comparison report")]
    public string? OutputPath { get; set; }

    [CommandOption("-f|--format")]
    [Description("Report format (Markdown, JSON, HTML)")]
    [DefaultValue("Markdown")]
    public string Format { get; set; } = "Markdown";
}
