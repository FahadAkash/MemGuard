using Spectre.Console.Cli;
using System.ComponentModel;

namespace MemGuard.Cli.Models;

public sealed class FixDumpSettings : CommandSettings
{
    [CommandArgument(0, "<dumpPath>")]
    [Description("Path to the memory dump file")]
    public string DumpPath { get; set; } = string.Empty;

    [CommandOption("--project")]
    [Description("Path to the project folder to fix")]
    public string? ProjectPath { get; set; }

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

    [CommandOption("--dry-run")]
    [Description("Show fixes without applying them")]
    [DefaultValue(false)]
    public bool DryRun { get; set; }

    [CommandOption("--auto-apply")]
    [Description("Apply fixes without confirmation")]
    [DefaultValue(false)]
    public bool AutoApply { get; set; }

    [CommandOption("-o|--output")]
    [Description("Path to save the analysis report")]
    public string? OutputPath { get; set; }
}
