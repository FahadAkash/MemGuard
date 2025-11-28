using MemGuard.Core.Interfaces;
using System.Text.Json;

namespace MemGuard.Core.Agent.Tools;

/// <summary>
/// Tool for reading file contents
/// </summary>
public class ReadFileTool : AgentTool
{
    private readonly IFileManager _fileManager;

    public ReadFileTool(IFileManager fileManager)
    {
        _fileManager = fileManager;
    }

    public override string Name => "read_file";
    public override string Description => "Read the contents of a file. Returns the file content as text.";
    public override string Category => "File Operations";
    public override string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""path"": {
      ""type"": ""string"",
      ""description"": ""Absolute or relative path to the file""
    },
    ""maxLines"": {
      ""type"": ""integer"",
      ""description"": ""Maximum number of lines to read (optional, default: all)"",
      ""default"": -1
    }
  },
  ""required"": [""path""]
}";

    protected override async Task<ToolResult> ExecuteInternalAsync(string parameters, CancellationToken cancellationToken)
    {
        var args = DeserializeParameters<ReadFileArgs>(parameters);
        if (args == null || string.IsNullOrWhiteSpace(args.Path))
        {
            return ToolResult.Failure(Name, "Path parameter is required");
        }

        if (!File.Exists(args.Path))
        {
            return ToolResult.Failure(Name, $"File not found: {args.Path}");
        }

        var content = await _fileManager.ReadFileAsync(args.Path);
        
        // Limit lines if specified
        if (args.MaxLines > 0)
        {
            var lines = content.Split('\n');
            if (lines.Length > args.MaxLines)
            {
                content = string.Join('\n', lines.Take(args.MaxLines));
                content += $"\n\n... ({lines.Length - args.MaxLines} more lines)";
            }
        }

        var metadata = new Dictionary<string, object>
        {
            ["filePath"] = args.Path,
            ["fileSize"] = new FileInfo(args.Path).Length,
            ["lineCount"] = content.Split('\n').Length
        };

        return ToolResult.CreateSuccess(Name, content, metadata);
    }

    private class ReadFileArgs
    {
        public string Path { get; set; } = string.Empty;
        public int MaxLines { get; set; } = -1;
    }
}
