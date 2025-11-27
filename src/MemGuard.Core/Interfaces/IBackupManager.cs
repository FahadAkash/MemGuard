namespace MemGuard.Core.Interfaces;

/// <summary>
/// Interface for managing backups and restore operations
/// </summary>
public interface IBackupManager
{
    /// <summary>
    /// Create a backup of files before modification
    /// </summary>
    /// <param name="files">Files to backup</param>
    /// <param name="metadata">Optional metadata about the backup</param>
    /// <returns>Backup ID</returns>
    Task<string> CreateBackupAsync(IEnumerable<string> files, string? metadata = null);
    
    /// <summary>
    /// Restore files from a backup
    /// </summary>
    /// <param name="backupId">Backup ID to restore</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RestoreBackupAsync(string backupId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// List all available backups
    /// </summary>
    /// <returns>List of backup IDs with metadata</returns>
    Task<IEnumerable<BackupInfo>> ListBackupsAsync();
    
    /// <summary>
    /// Delete a backup
    /// </summary>
    /// <param name="backupId">Backup ID to delete</param>
    Task DeleteBackupAsync(string backupId);
}

/// <summary>
/// Information about a backup with project tracking
/// </summary>
public record BackupInfo(
    string BackupId,
    DateTime Timestamp,
    IReadOnlyList<string> Files,
    string? Metadata)
{
    /// <summary>
    /// Project root directory where backup was created
    /// </summary>
    public string? ProjectRoot { get; init; }
    
    /// <summary>
    /// Whether this backup belongs to the current project
    /// </summary>
    public bool IsCurrentProject { get; init; } = true;
}
