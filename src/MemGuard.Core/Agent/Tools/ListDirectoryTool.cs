namespace MemGuard.Core.Agent.Tools;

/// <summary>
/// Tool for listing directory contents
/// </summary>
public class ListDirectoryTool : AgentTool
{
    public override string Name => "list_directory";
    public override string Description => "List files and subdirectories in a directory.";
    public override string Category => "File Operations";
    public override string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""path"": {
      ""type"": ""string"",
      ""description"": ""Absolute or relative path to the directory""
    },
    ""recursive"": {
      ""type"": ""boolean"",
      ""description"": ""Whether to list recursively (default: false)"",
      ""default"": false
    },
    ""pattern"": {
      ""type"": ""string"",
      ""description"": ""File pattern to match (e.g., '*.cs', default: all files)"",
      ""default"": ""*""
    }
  },
  ""required"": [""path""]
}";

    protected override Task<ToolResult> ExecuteInternalAsync(string parameters, CancellationToken cancellationToken)
    {
        var args = DeserializeParameters<ListDirectoryArgs>(parameters);
        if (args == null || string.IsNullOrWhiteSpace(args.Path))
        {
            return Task.FromResult(ToolResult.Failure(Name, "Path parameter is required"));
        }

        if (!Directory.Exists(args.Path))
        {
            return Task.FromResult(ToolResult.Failure(Name, $"Directory not found: {args.Path}"));
        }

        var searchOption = args.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var pattern = string.IsNullOrWhiteSpace(args.Pattern) ? "*" : args.Pattern;

        var files = Directory.GetFiles(args.Path, pattern, searchOption);
        var directories = Directory.GetDirectories(args.Path, "*", searchOption);

        var output = new System.Text.StringBuilder();
        output.AppendLine($"Directory: {args.Path}");
        output.AppendLine();

        if (directories.Length > 0)
        {
            output.AppendLine("Subdirectories:");
            foreach (var dir in directories.OrderBy(d => d))
            {
                var relativePath = Path.GetRelativePath(args.Path, dir);
                output.AppendLine($"  📁 {relativePath}");
            }
            output.AppendLine();
        }

        if (files.Length > 0)
        {
            output.AppendLine("Files:");
            foreach (var file in files.OrderBy(f => f))
            {
                var relativePath = Path.GetRelativePath(args.Path, file);
                var fileInfo = new FileInfo(file);
                output.AppendLine($"  📄 {relativePath} ({FormatFileSize(fileInfo.Length)})");
            }
        }
        else
        {
            output.AppendLine("No files found.");
        }

        var metadata = new Dictionary<string, object>
        {
            ["directoryCount"] = directories.Length,
            ["fileCount"] = files.Length,
            ["totalSize"] = files.Sum(f => new FileInfo(f).Length)
        };

        return Task.FromResult(ToolResult.CreateSuccess(Name, output.ToString(), metadata));
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private class ListDirectoryArgs
    {
        public string Path { get; set; } = string.Empty;
        public bool Recursive { get; set; } = false;
        public string Pattern { get; set; } = "*";
    }
}
