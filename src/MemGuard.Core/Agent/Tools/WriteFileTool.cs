using MemGuard.Core.Interfaces;

namespace MemGuard.Core.Agent.Tools;

/// <summary>
/// Tool for writing or modifying file contents
/// </summary>
public class WriteFileTool : AgentTool
{
    private readonly IFileManager _fileManager;
    private readonly IBackupManager? _backupManager;

    public WriteFileTool(IFileManager fileManager, IBackupManager? backupManager = null)
    {
        _fileManager = fileManager;
        _backupManager = backupManager;
    }

    public override string Name => "write_file";
    public override string Description => "Write content to a file. Creates the file if it doesn't exist, overwrites if it does. A backup is automatically created.";
    public override string Category => "File Operations";
    public override string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""path"": {
      ""type"": ""string"",
      ""description"": ""Absolute or relative path to the file""
    },
    ""content"": {
      ""type"": ""string"",
      ""description"": ""Content to write to the file""
    },
    ""createBackup"": {
      ""type"": ""boolean"",
      ""description"": ""Whether to create a backup before writing (default: true)"",
      ""default"": true
    }
  },
  ""required"": [""path"", ""content""]
}";

    protected override async Task<ToolResult> ExecuteInternalAsync(string parameters, CancellationToken cancellationToken)
    {
        var args = DeserializeParameters<WriteFileArgs>(parameters);
        if (args == null || string.IsNullOrWhiteSpace(args.Path))
        {
            return ToolResult.Failure(Name, "Path parameter is required");
        }

        if (args.Content == null)
        {
            return ToolResult.Failure(Name, "Content parameter is required");
        }

        string? backupId = null;

        // Create backup if file exists and backup is requested
        if (args.CreateBackup && File.Exists(args.Path) && _backupManager != null)
        {
            backupId = await _backupManager.CreateBackupAsync(new[] { args.Path }, $"Auto-backup before write to {Path.GetFileName(args.Path)}");
        }

        // Write the file
        await _fileManager.WriteFileAsync(args.Path, args.Content);

        var metadata = new Dictionary<string, object>
        {
            ["filePath"] = args.Path,
            ["bytesWritten"] = System.Text.Encoding.UTF8.GetByteCount(args.Content),
            ["backupCreated"] = backupId != null
        };

        if (backupId != null)
        {
            metadata["backupId"] = backupId;
        }

        var output = $"Successfully wrote to {args.Path}";
        if (backupId != null)
        {
            output += $"\nBackup created: {backupId}";
        }

        return ToolResult.CreateSuccess(Name, output, metadata);
    }

    private class WriteFileArgs
    {
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool CreateBackup { get; set; } = true;
    }
}
