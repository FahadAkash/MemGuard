using Spectre.Console;
using Spectre.Console.Cli;
using MemGuard.Core.Interfaces;
using MemGuard.Infrastructure;
using MemGuard.Cli.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MemGuard.Cli.Commands;

public sealed class RestoreCommand : AsyncCommand<RestoreSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RestoreSettings settings)
    {
        try
        {
            var services = new ServiceCollection();
            services.AddSingleton<IBackupManager, BackupManager>();
            var serviceProvider = services.BuildServiceProvider();
            var backupManager = serviceProvider.GetRequiredService<IBackupManager>();

            // List backups
            if (settings.List)
            {
                var backups = await backupManager.ListBackupsAsync();
                
                if (!backups.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No backups found[/]");
                    return 0;
                }

                var table = new Table();
                table.AddColumn("Backup ID");
                table.AddColumn("Timestamp");
                table.AddColumn("Files");
                table.AddColumn("Metadata");

                foreach (var backup in backups)
                {
                    table.AddRow(
                        backup.BackupId,
                        backup.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        backup.Files.Count.ToString(),
                        backup.Metadata ?? "-");
                }

                AnsiConsole.Write(table);
                return 0;
            }

            // Restore backup
            string? backupId = settings.BackupId;
            
            if (settings.Latest)
            {
                var backups = await backupManager.ListBackupsAsync();
                backupId = backups.FirstOrDefault()?.BackupId;
                
                if (backupId == null)
                {
                    AnsiConsole.MarkupLine("[red]No backups found[/]");
                    return 1;
                }
            }

            if (string.IsNullOrEmpty(backupId))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Specify --backup-id or --latest");
                return 1;
            }

            if (!AnsiConsole.Confirm($"Restore backup {backupId}?"))
            {
                AnsiConsole.MarkupLine("[yellow]Restore cancelled[/]");
                return 0;
            }

            await backupManager.RestoreBackupAsync(backupId);
            AnsiConsole.MarkupLine($"[green]Successfully restored backup {backupId}[/]");
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }
    }
}
