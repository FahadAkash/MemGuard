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
        string? oldContent = null;

        // Read old content if file exists (for diff)
        if (File.Exists(args.Path))
        {
            oldContent = await _fileManager.ReadFileAsync(args.Path);
            
            // Create backup if requested
            if (args.CreateBackup && _backupManager != null)
            {
                backupId = await _backupManager.CreateBackupAsync(new[] { args.Path }, $"Auto-backup before write to {Path.GetFileName(args.Path)}");
            }
        }

        // Write the file
        await _fileManager.WriteFileAsync(args.Path, args.Content);

        var metadata = new Dictionary<string, object>
        {
            ["filePath"] = args.Path,
            ["bytesWritten"] = System.Text.Encoding.UTF8.GetByteCount(args.Content),
            ["backupCreated"] = backupId != null,
            ["fileModified"] = oldContent != null
        };

        if (backupId != null)
        {
            metadata["backupId"] = backupId;
        }

        // Build output with diff
        var output = new System.Text.StringBuilder();
        
        if (oldContent != null)
        {
            output.AppendLine($"✏️ Modified: {args.Path}");
            output.AppendLine();
            
            // Generate simple diff
            var diff = GenerateSimpleDiff(oldContent, args.Content);
            output.AppendLine("📊 Changes:");
            output.AppendLine(diff);
        }
        else
        {
            output.AppendLine($"✨ Created: {args.Path}");
            output.AppendLine($"📏 Size: {args.Content.Length} characters");
        }

        if (backupId != null)
        {
            output.AppendLine();
            output.AppendLine($"💾 Backup: {backupId}");
        }

        return ToolResult.CreateSuccess(Name, output.ToString(), metadata);
    }

    private string GenerateSimpleDiff(string oldContent, string newContent)
    {
        var oldLines = oldContent.Split('\n');
        var newLines = newContent.Split('\n');
        var diff = new System.Text.StringBuilder();
        
        int addedLines = 0;
        int removedLines = 0;
        int unchangedLines = 0;
        
        // Simple line-by-line comparison
        int maxLines = Math.Max(oldLines.Length, newLines.Length);
        for (int i = 0; i < Math.Min(oldLines.Length, newLines.Length); i++)
        {
            if (oldLines[i] != newLines[i])
            {
                if (i < oldLines.Length) removedLines++;
                if (i < newLines.Length) addedLines++;
            }
            else
            {
                unchangedLines++;
            }
        }
        
        // Count remaining lines
        if (newLines.Length > oldLines.Length)
            addedLines += (newLines.Length - oldLines.Length);
        else if (oldLines.Length > newLines.Length)
            removedLines += (oldLines.Length - newLines.Length);
        
        diff.AppendLine($"  + {addedLines} additions");
        diff.AppendLine($"  - {removedLines} deletions");
        diff.AppendLine($"  = {unchangedLines} unchanged");
        diff.AppendLine();
        
        // Show first few changed lines
        diff.AppendLine("Preview (first 5 changes):");
        int changesShown = 0;
        for (int i = 0; i < Math.Min(oldLines.Length, newLines.Length) && changesShown < 5; i++)
        {
            if (oldLines[i] != newLines[i])
            {
                diff.AppendLine($"  Line {i + 1}:");
                diff.AppendLine($"    - {oldLines[i].Trim()}");
                diff.AppendLine($"    + {newLines[i].Trim()}");
                changesShown++;
            }
        }
        
        return diff.ToString();
    }

    private class WriteFileArgs
    {
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool CreateBackup { get; set; } = true;
    }
}
