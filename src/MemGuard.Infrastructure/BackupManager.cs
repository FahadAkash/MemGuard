using System.Text.Json;
using MemGuard.Core.Interfaces;

namespace MemGuard.Infrastructure;

/// <summary>
/// Manages backups and restore operations
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

        var fileList = new List<string>();

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
            fileList.Add(file);
        }

        // Save metadata
        var metadataPath = Path.Combine(backupDir, "metadata.json");
        var metadataObj = new
        {
            BackupId = backupId,
            Timestamp = DateTime.Now,
            Files = fileList,
            Metadata = metadata
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

        foreach (var originalFile in metadata.Files)
        {
            var fileName = Path.GetFileName(originalFile);
            var backupFile = Path.Combine(backupDir, fileName);

            if (File.Exists(backupFile))
            {
                File.Copy(backupFile, originalFile, true);
            }
        }
    }

    public async Task<IEnumerable<BackupInfo>> ListBackupsAsync()
    {
        var backups = new List<BackupInfo>();

        if (!Directory.Exists(_backupRoot))
            return backups;

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
                    backups.Add(new BackupInfo(
                        metadata.BackupId,
                        metadata.Timestamp,
                        metadata.Files,
                        metadata.Metadata));
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

    private class BackupMetadata
    {
        public string BackupId { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public List<string> Files { get; set; } = new();
        public string? Metadata { get; set; }
    }
}
