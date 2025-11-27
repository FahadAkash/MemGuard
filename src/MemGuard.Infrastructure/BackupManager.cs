using System.Text.Json;
using MemGuard.Core.Interfaces;

namespace MemGuard.Infrastructure;

/// <summary>
/// Manages backups and restore operations with project-specific isolation
/// </summary>
public class BackupManager : IBackupManager
{
    private readonly string _backupRoot;

    public BackupManager(string? backupRoot = null)
    {
        _backupRoot = backupRoot ?? Path.Combine(Environment.CurrentDirectory, ".memguard", "backups");
        Directory.CreateDirectory(_backupRoot);
    }

    public async Task<string> CreateBackupAsync(IEnumerable<string> files, string? metadata = null)
    {
        var backupId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupDir = Path.Combine(_backupRoot, backupId);
        Directory.CreateDirectory(backupDir);

        var fileList = new List<BackupFileInfo>();

        // Determine project root from first file
        var firstFile = files.FirstOrDefault();
        var projectRoot = firstFile != null ? GetProjectRoot(firstFile) : Environment.CurrentDirectory;

        foreach (var file in files)
        {
            if (!File.Exists(file))
                continue;

            var relativePath = Path.GetFileName(file);
            var backupPath = Path.Combine(backupDir, relativePath);
            
            // Create subdirectories if needed
            var backupFileDir = Path.GetDirectoryName(backupPath);
            if (backupFileDir != null)
                Directory.CreateDirectory(backupFileDir);

            File.Copy(file, backupPath, true);
            
            // Store both original path and relative path
            fileList.Add(new BackupFileInfo
            {
                OriginalPath = file,
                RelativePath = relativePath,
                BackupPath = backupPath
            });
        }

        // Save metadata with project information
        var metadataPath = Path.Combine(backupDir, "metadata.json");
        var metadataObj = new BackupMetadata
        {
            BackupId = backupId,
            Timestamp = DateTime.Now,
            Files = fileList,
            Metadata = metadata,
            ProjectRoot = projectRoot,
            MachineName = Environment.MachineName,
            UserName = Environment.UserName
        };
        
        await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadataObj, new JsonSerializerOptions { WriteIndented = true }));

        return backupId;
    }

    public async Task RestoreBackupAsync(string backupId, CancellationToken cancellationToken = default)
    {
        var backupDir = Path.Combine(_backupRoot, backupId);
        if (!Directory.Exists(backupDir))
            throw new DirectoryNotFoundException($"Backup {backupId} not found");

        var metadataPath = Path.Combine(backupDir, "metadata.json");
        if (!File.Exists(metadataPath))
            throw new FileNotFoundException("Backup metadata not found");

        var metadataJson = await File.ReadAllTextAsync(metadataPath, cancellationToken);
        var metadata = JsonSerializer.Deserialize<BackupMetadata>(metadataJson);

        if (metadata?.Files == null)
            throw new InvalidOperationException("Invalid backup metadata");

        // Validate project root matches (safety check)
        if (!string.IsNullOrEmpty(metadata.ProjectRoot))
        {
            var currentProjectRoot = GetProjectRoot(Environment.CurrentDirectory);
            if (!IsSameProject(metadata.ProjectRoot, currentProjectRoot))
            {
                throw new InvalidOperationException(
                    $"Backup is from a different project!\n" +
                    $"Backup project: {metadata.ProjectRoot}\n" +
                    $"Current project: {currentProjectRoot}\n" +
                    $"To restore this backup, navigate to the correct project directory first.");
            }
        }

        // Restore files
        foreach (var fileInfo in metadata.Files)
        {
            var fileName = Path.GetFileName(fileInfo.OriginalPath);
            var backupFile = Path.Combine(backupDir, fileName);

            if (File.Exists(backupFile) && File.Exists(fileInfo.OriginalPath))
            {
                File.Copy(backupFile, fileInfo.OriginalPath, true);
            }
        }
    }

    public async Task<IEnumerable<BackupInfo>> ListBackupsAsync()
    {
        var backups = new List<BackupInfo>();

        if (!Directory.Exists(_backupRoot))
            return backups;

        var currentProjectRoot = GetProjectRoot(Environment.CurrentDirectory);

        foreach (var backupDir in Directory.GetDirectories(_backupRoot))
        {
            var metadataPath = Path.Combine(backupDir, "metadata.json");
            if (!File.Exists(metadataPath))
                continue;

            try
            {
                var metadataJson = await File.ReadAllTextAsync(metadataPath);
                var metadata = JsonSerializer.Deserialize<BackupMetadata>(metadataJson);

                if (metadata != null)
                {
                    // Only show backups from current project
                    var isCurrentProject = string.IsNullOrEmpty(metadata.ProjectRoot) || 
                                          IsSameProject(metadata.ProjectRoot, currentProjectRoot);

                    var fileList = metadata.Files?.Select(f => f.OriginalPath).ToList() ?? new List<string>();
                    
                    backups.Add(new BackupInfo(
                        metadata.BackupId,
                        metadata.Timestamp,
                        fileList,
                        metadata.Metadata)
                    {
                        ProjectRoot = metadata.ProjectRoot,
                        IsCurrentProject = isCurrentProject
                    });
                }
            }
            catch
            {
                // Skip invalid backups
            }
        }

        return backups.OrderByDescending(b => b.Timestamp);
    }

    public Task DeleteBackupAsync(string backupId)
    {
        var backupDir = Path.Combine(_backupRoot, backupId);
        if (Directory.Exists(backupDir))
        {
            Directory.Delete(backupDir, true);
        }
        return Task.CompletedTask;
    }

    private static string GetProjectRoot(string path)
    {
        // Try to find project root by looking for .git, .sln, or src folder
        var directory = File.Exists(path) ? Path.GetDirectoryName(path) : path;
        
        while (!string.IsNullOrEmpty(directory))
        {
            if (Directory.Exists(Path.Combine(directory, ".git")) ||
                Directory.GetFiles(directory, "*.sln").Length > 0 ||
                Directory.Exists(Path.Combine(directory, "src")))
            {
                return directory;
            }
            
            directory = Path.GetDirectoryName(directory);
        }
        
        // Fallback to current directory
        return Environment.CurrentDirectory;
    }

    private static bool IsSameProject(string path1, string path2)
    {
        // Normalize paths for comparison
        var normalized1 = Path.GetFullPath(path1).TrimEnd(Path.DirectorySeparatorChar);
        var normalized2 = Path.GetFullPath(path2).TrimEnd(Path.DirectorySeparatorChar);
        
        return normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase);
    }

    private class BackupMetadata
    {
        public string BackupId { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public List<BackupFileInfo>? Files { get; set; }
        public string? Metadata { get; set; }
        public string? ProjectRoot { get; set; }
        public string? MachineName { get; set; }
        public string? UserName { get; set; }
    }

    private class BackupFileInfo
    {
        public string OriginalPath { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public string BackupPath { get; set; } = "";
    }
}
