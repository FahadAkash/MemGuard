using Spectre.Console;
using Spectre.Console.Cli;
using MemGuard.Core.Interfaces;
using MemGuard.Infrastructure;
using MemGuard.Cli.Models;

namespace MemGuard.Cli.Commands;

public sealed class RestoreCommand : AsyncCommand<RestoreSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RestoreSettings settings)
    {
        try
        {
            AnsiConsole.Write(new FigletText("MemGuard Restore").Color(Color.Green));
            AnsiConsole.MarkupLine("[grey]Backup & Restore System[/]");
            AnsiConsole.WriteLine();

            var backupManager = new BackupManager();

            // List backups
            if (settings.List)
            {
                var backups = await backupManager.ListBackupsAsync();
                var backupList = backups.ToList();

                if (backupList.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No backups found[/]");
                    return 0;
                }

                var table = new Table();
                table.Border(TableBorder.Rounded);
                table.AddColumn("Backup ID");
                table.AddColumn("Created");
                table.AddColumn("Files");
                table.AddColumn("Project");
                table.AddColumn("Status");

                foreach (var backup in backupList)
                {
                    var projectName = backup.ProjectRoot != null 
                        ? Path.GetFileName(backup.ProjectRoot) 
                        : "Unknown";
                    
                    var status = backup.IsCurrentProject 
                        ? "[green]Current[/]" 
                        : "[yellow]Different[/]";

                    table.AddRow(
                        backup.BackupId,
                        backup.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        backup.Files.Count.ToString(),
                        projectName.EscapeMarkup(),
                        status
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[grey]Tip: Use --latest to restore the most recent backup[/]");
                AnsiConsole.MarkupLine("[grey]     Use --backup-id <id> to restore a specific backup[/]");
                return 0;
            }

            // Restore backup
            string? backupId = null;

            if (settings.Latest)
            {
                var backups = await backupManager.ListBackupsAsync();
                var currentProjectBackups = backups.Where(b => b.IsCurrentProject).ToList();
                
                if (currentProjectBackups.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No backups found for current project[/]");
                    
                    var allBackups = backups.ToList();
                    if (allBackups.Count > 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]Found backups from other projects:[/]");
                        foreach (var backup in allBackups.Take(3))
                        {
                            var projectName = backup.ProjectRoot != null 
                                ? Path.GetFileName(backup.ProjectRoot) 
                                : "Unknown";
                            AnsiConsole.MarkupLine($"  • {backup.BackupId} from {projectName}");
                        }
                        AnsiConsole.MarkupLine("[grey]Navigate to the correct project directory to restore these backups[/]");
                    }
                    
                    return 1;
                }
                
                backupId = currentProjectBackups.First().BackupId;
            }
            else if (!string.IsNullOrEmpty(settings.BackupId))
            {
                backupId = settings.BackupId;
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Please specify --latest or --backup-id");
                return 1;
            }

            // Get backup info
            var allBackupsForConfirm = await backupManager.ListBackupsAsync();
            var selectedBackup = allBackupsForConfirm.FirstOrDefault(b => b.BackupId == backupId);

            if (selectedBackup == null)
            {
                AnsiConsole.MarkupLine($"[red]Backup {backupId} not found[/]");
                return 1;
            }

            // Warn if different project
            if (!selectedBackup.IsCurrentProject)
            {
                AnsiConsole.MarkupLine("[yellow]⚠ WARNING:[/] This backup is from a different project!");
                AnsiConsole.MarkupLine($"[yellow]Backup project:[/] {selectedBackup.ProjectRoot}");
                AnsiConsole.MarkupLine($"[yellow]Current directory:[/] {Environment.CurrentDirectory}");
                AnsiConsole.WriteLine();
                
                if (!AnsiConsole.Confirm("Are you sure you want to restore this backup?", false))
                {
                    AnsiConsole.MarkupLine("[yellow]Restore cancelled[/]");
                    return 0;
                }
            }

            // Show what will be restored
            AnsiConsole.MarkupLine($"[yellow]Restoring backup:[/] {backupId}");
            AnsiConsole.MarkupLine($"[yellow]Created:[/] {selectedBackup.Timestamp}");
            AnsiConsole.MarkupLine($"[yellow]Files to restore:[/] {selectedBackup.Files.Count}");
            
            foreach (var file in selectedBackup.Files.Take(5))
            {
                AnsiConsole.MarkupLine($"  • {Path.GetFileName(file).EscapeMarkup()}");
            }
            
            if (selectedBackup.Files.Count > 5)
            {
                AnsiConsole.MarkupLine($"  ... and {selectedBackup.Files.Count - 5} more");
            }
            
            AnsiConsole.WriteLine();

            // Confirm
            if (!AnsiConsole.Confirm("Proceed with restore?"))
            {
                AnsiConsole.MarkupLine("[yellow]Restore cancelled[/]");
                return 0;
            }

            // Restore
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Restoring files...", async ctx =>
                {
                    await backupManager.RestoreBackupAsync(backupId);
                });

            AnsiConsole.MarkupLine($"[green]✓ Successfully restored {selectedBackup.Files.Count} files from backup {backupId}[/]");
            return 0;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("different project"))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }
    }
}
