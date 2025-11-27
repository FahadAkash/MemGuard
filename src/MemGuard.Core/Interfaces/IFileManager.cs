namespace MemGuard.Core.Interfaces;

/// <summary>
/// Manages file operations for the AI agent
/// </summary>
public interface IFileManager
{
    /// <summary>
    /// Read a file's content
    /// </summary>
    Task<string> ReadFileAsync(string path);

    /// <summary>
    /// Write content to a file
    /// </summary>
    Task WriteFileAsync(string path, string content);

    /// <summary>
    /// Delete a file
    /// </summary>
    Task DeleteFileAsync(string path);

    /// <summary>
    /// List files matching a pattern
    /// </summary>
    Task<List<string>> ListFilesAsync(string directory, string pattern = "*.*");

    /// <summary>
    /// Search for text in files
    /// </summary>
    Task<Dictionary<string, List<string>>> SearchInFilesAsync(string directory, string searchText);

    /// <summary>
    /// Get project structure
    /// </summary>
    Task<Dictionary<string, object>> GetProjectStructureAsync(string projectPath);
}
