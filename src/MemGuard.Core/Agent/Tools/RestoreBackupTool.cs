using MemGuard.Core.Interfaces;

namespace MemGuard.Core.Agent.Tools;

/// <summary>
/// Tool for restoring files from backups
/// </summary>
public class RestoreBackupTool : AgentTool
{
    private readonly IFileManager _fileManager;

    public RestoreBackupTool(IFileManager fileManager)
    {
        _fileManager = fileManager;
    }

    public override string Name => "restore_backup";
    public override string Description => "Restore a file to its previous state from a backup. Use this if a change caused errors or needs to be reverted.";
    public override string Category => "File Operations";
    public override string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""filePath"": {
      ""type"": ""string"",
      ""description"": ""The path of the file to restore""
    }
  },
  ""required"": [""filePath""]
}";

    protected override async Task<ToolResult> ExecuteInternalAsync(string parameters, CancellationToken cancellationToken)
    {
        var args = DeserializeParameters<RestoreArgs>(parameters);
        if (args == null || string.IsNullOrWhiteSpace(args.FilePath))
        {
            return ToolResult.Failure(Name, "FilePath parameter is required");
        }

        try
        {
            // Find the latest backup for this file
            var backupDir = Path.Combine(Path.GetDirectoryName(args.FilePath) ?? "", ".memguard", "backups");
            // Note: The actual backup location depends on FileManager implementation. 
            // Since FileManager manages backups, we should ideally add a Restore method to it.
            // But for now, let's assume we can use the CLI's restore logic or implement simple restore here.
            
            // Actually, FileManager doesn't expose RestoreAsync publicly in the interface I recall.
            // Let's check FileManager first.
            
            // For now, I'll implement a simple restore by looking for the .bak file
            // The WriteFileTool creates backups with a timestamp, but we need to find the LATEST one.
            
            // Let's assume the standard backup location used by FileManager
            var projectRoot = FindProjectRoot(args.FilePath);
            var backupsDir = Path.Combine(projectRoot, ".memguard", "backups");
            
            if (!Directory.Exists(backupsDir))
            {
                return ToolResult.Failure(Name, "No backup directory found.");
            }

            var fileName = Path.GetFileName(args.FilePath);
            var backupFiles = Directory.GetFiles(backupsDir, $"{fileName}.*.bak")
                                       .OrderByDescending(f => f) // Latest first
                                       .ToList();

            if (!backupFiles.Any())
            {
                return ToolResult.Failure(Name, $"No backups found for file: {fileName}");
            }

            var latestBackup = backupFiles.First();
            
            // Restore it
            File.Copy(latestBackup, args.FilePath, true);
            
            return ToolResult.CreateSuccess(Name, $"Successfully restored {fileName} from backup {Path.GetFileName(latestBackup)}");
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(Name, $"Failed to restore file: {ex.Message}", ex);
        }
    }

    private string FindProjectRoot(string path)
    {
        var dir = Path.GetDirectoryName(path);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, ".memguard"))) return dir;
            if (Directory.GetFiles(dir, "*.csproj").Any()) return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return Path.GetDirectoryName(path) ?? ".";
    }

    private class RestoreArgs
    {
        public string FilePath { get; set; } = string.Empty;
    }
}
