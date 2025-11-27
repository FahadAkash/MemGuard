using MemGuard.Core.Interfaces;
using System.Text;

namespace MemGuard.Infrastructure;

/// <summary>
/// File manager for AI agent operations
/// </summary>
public class FileManager : IFileManager
{
    public async Task<string> ReadFileAsync(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        return await File.ReadAllTextAsync(path);
    }

    public async Task WriteFileAsync(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, content);
    }

    public Task DeleteFileAsync(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }

    public Task<List<string>> ListFilesAsync(string directory, string pattern = "*.*")
    {
        if (!Directory.Exists(directory))
            return Task.FromResult(new List<string>());

        var files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(directory, f))
            .ToList();

        return Task.FromResult(files);
    }

    public async Task<Dictionary<string, List<string>>> SearchInFilesAsync(string directory, string searchText)
    {
        var results = new Dictionary<string, List<string>>();

        if (!Directory.Exists(directory))
            return results;

        var files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var lines = await File.ReadAllLinesAsync(file);
            var matchingLines = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    matchingLines.Add($"Line {i + 1}: {lines[i].Trim()}");
                }
            }

            if (matchingLines.Count > 0)
            {
                results[Path.GetRelativePath(directory, file)] = matchingLines;
            }
        }

        return results;
    }

    public async Task<Dictionary<string, object>> GetProjectStructureAsync(string projectPath)
    {
        var structure = new Dictionary<string, object>();

        if (!Directory.Exists(projectPath))
            return structure;

        // Get .csproj files
        var projects = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories);
        structure["Projects"] = projects.Select(p => Path.GetRelativePath(projectPath, p)).ToList();

        // Get .cs files
        var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);
        structure["CSharpFiles"] = csFiles.Length;

        // Get directories
        var dirs = Directory.GetDirectories(projectPath, "*", SearchOption.TopDirectoryOnly);
        structure["Directories"] = dirs.Select(d => Path.GetFileName(d)).ToList();

        // Get file count by type
        var fileTypes = new Dictionary<string, int>();
        foreach (var file in Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file);
            if (!string.IsNullOrEmpty(ext))
            {
                fileTypes[ext] = fileTypes.GetValueOrDefault(ext, 0) + 1;
            }
        }
        structure["FileTypes"] = fileTypes;

        return structure;
    }
}
