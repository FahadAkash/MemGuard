using Spectre.Console.Cli;
using System.ComponentModel;

namespace MemGuard.Cli.Models;

public sealed class AnalyzeDumpSettings : CommandSettings
{
    [CommandArgument(0, "<dumpPath>")]
    [Description("Path to the memory dump file")]
    public string DumpPath { get; set; } = string.Empty;

    [CommandOption("-o|--output")]
    [Description("Path to save the output report")]
    public string? OutputPath { get; set; }

    [CommandOption("--provider")]
    [Description("AI Provider to use (Ollama or Gemini)")]
    [DefaultValue("Gemini")]
    public string Provider { get; set; } = "Gemini";

    [CommandOption("--model")]
    [Description("Model name (e.g., llama3.2, gemini-1.5-flash)")]
    public string? Model { get; set; }

    [CommandOption("--api-key")]
    [Description("API Key for Gemini")]
    public string? ApiKey { get; set; }

    [CommandOption("-f|--format")]
    [Description("Report format (Markdown, PDF, HTML)")]
    [DefaultValue("Markdown")]
    public string Format { get; set; } = "Markdown";

    [CommandOption("--export-json")]
    [Description("Also export analysis as JSON")]
    [DefaultValue(false)]
    public bool ExportJson { get; set; }
}
