using Spectre.Console.Cli;
using System.ComponentModel;

namespace MemGuard.Cli.Models;

public sealed class RestoreSettings : CommandSettings
{
    [CommandOption("--list")]
    [Description("List all available backups")]
    [DefaultValue(false)]
    public bool List { get; set; }

    [CommandOption("--backup-id")]
    [Description("Backup ID to restore")]
    public string? BackupId { get; set; }

    [CommandOption("--latest")]
    [Description("Restore the latest backup")]
    [DefaultValue(false)]
    public bool Latest { get; set; }
}
