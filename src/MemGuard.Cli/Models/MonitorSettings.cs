using Spectre.Console.Cli;
using System.ComponentModel;

namespace MemGuard.Cli.Models;

public sealed class MonitorSettings : CommandSettings
{
    [CommandOption("--process")]
    [Description("Process name to monitor")]
    public string? ProcessName { get; set; }

    [CommandOption("--pid")]
    [Description("Process ID to monitor")]
    public int? ProcessId { get; set; }

    [CommandOption("--interval")]
    [Description("Monitoring interval in seconds")]
    [DefaultValue(5)]
    public int IntervalSeconds { get; set; } = 5;

    [CommandOption("--duration")]
    [Description("Total monitoring duration in seconds (0 = infinite)")]
    [DefaultValue(0)]
    public int DurationSeconds { get; set; }

    [CommandOption("--alert-threshold")]
    [Description("Alert when memory exceeds this threshold in MB")]
    public long? AlertThresholdMB { get; set; }

    [CommandOption("-o|--output")]
    [Description("Output file for monitoring data (JSON)")]
    public string? OutputPath { get; set; }
}
