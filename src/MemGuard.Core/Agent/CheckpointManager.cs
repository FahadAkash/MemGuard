using System.Text.Json;
using MemGuard.Core.Interfaces;

namespace MemGuard.Core.Agent;

/// <summary>
/// Manages saving and loading of agent checkpoints
/// </summary>
public class CheckpointManager
{
    private readonly string _baseDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public CheckpointManager(string baseDirectory)
    {
        _baseDirectory = Path.Combine(baseDirectory, ".memguard", "checkpoints");
        _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        
        EnsureDirectoryExists();
    }

    public CheckpointManager() : this(Environment.CurrentDirectory)
    {
    }

    /// <summary>
    /// Save the current agent state as a checkpoint
    /// </summary>
    public async Task<string> SaveCheckpointAsync(AgentState state, string name)
    {
        EnsureDirectoryExists();
        
        var filename = $"{name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        var path = Path.Combine(_baseDirectory, filename);
        
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
        
        return path;
    }

    /// <summary>
    /// Load a checkpoint by name (or path)
    /// </summary>
    public async Task<AgentState?> LoadCheckpointAsync(string nameOrPath)
    {
        string path = nameOrPath;
        
        if (!File.Exists(path))
        {
            // Try looking in checkpoints directory
            path = Path.Combine(_baseDirectory, nameOrPath);
            if (!path.EndsWith(".json")) path += ".json";
            
            if (!File.Exists(path))
            {
                // Try finding by prefix match if it's just a name
                var match = Directory.GetFiles(_baseDirectory, $"{nameOrPath}*.json")
                    .OrderByDescending(f => f)
                    .FirstOrDefault();
                    
                if (match != null) path = match;
                else return null;
            }
        }

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<AgentState>(json, _jsonOptions);
    }

    /// <summary>
    /// List all available checkpoints
    /// </summary>
    public IEnumerable<CheckpointInfo> ListCheckpoints()
    {
        EnsureDirectoryExists();
        
        return Directory.GetFiles(_baseDirectory, "*.json")
            .Select(f => new CheckpointInfo
            {
                Name = Path.GetFileNameWithoutExtension(f),
                Path = f,
                Created = File.GetCreationTime(f),
                Size = new FileInfo(f).Length
            })
            .OrderByDescending(c => c.Created);
    }

    /// <summary>
    /// Get the latest checkpoint
    /// </summary>
    public async Task<AgentState?> LoadLatestCheckpointAsync()
    {
        var latest = ListCheckpoints().FirstOrDefault();
        if (latest == null) return null;
        
        return await LoadCheckpointAsync(latest.Path);
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_baseDirectory))
        {
            Directory.CreateDirectory(_baseDirectory);
        }
    }
}

public class CheckpointInfo
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public DateTime Created { get; set; }
    public long Size { get; set; }
}
