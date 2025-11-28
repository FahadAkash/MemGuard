using Spectre.Console.Cli;
using System.ComponentModel;

namespace MemGuard.Cli.Models;

public sealed class AgentSettings : CommandSettings
{
    [CommandOption("--project")]
    [Description("Path to the project to work on")]
    public string? ProjectPath { get; set; }

    [CommandOption("--provider")]
    [Description("AI Provider (gemini, claude, grok, deepseek, ollama)")]
    [DefaultValue("claude")]
    public string Provider { get; set; } = "claude";

    [CommandOption("--model")]
    [Description("Model name (optional, uses provider default)")]
    public string? Model { get; set; }

    [CommandOption("--api-key")]
    [Description("API Key for the AI provider")]
    public string? ApiKey { get; set; }

    [CommandOption("--autonomous")]
    [Description("Run in autonomous mode (auto-fix without confirmation)")]
    [DefaultValue(false)]
    public bool Autonomous { get; set; }

    [CommandOption("--max-turns")]
    [Description("Maximum conversation turns")]
    [DefaultValue(50)]
    public int MaxTurns { get; set; } = 50;

    [CommandOption("--test")]
    [Description("Run agent loop test")]
    [DefaultValue(false)]
    public bool Test { get; set; }
}
