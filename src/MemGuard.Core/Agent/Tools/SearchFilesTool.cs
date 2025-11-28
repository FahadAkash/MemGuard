namespace MemGuard.Core.Agent.Tools;

/// <summary>
/// Tool for searching files by pattern
/// </summary>
public class SearchFilesTool : AgentTool
{
    public override string Name => "search_files";
    public override string Description => "Search for files matching a pattern in a directory tree. Useful for finding specific files or file types.";
    public override string Category => "File Operations";
    public override string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""path"": {
      ""type"": ""string"",
      ""description"": ""Root directory to search from""
    },
    ""pattern"": {
      ""type"": ""string"",
      ""description"": ""File pattern to search for (e.g., '*.cs', 'User*.cs')"",
      ""default"": ""*.*""
    },
    ""maxResults"": {
      ""type"": ""integer"",
      ""description"": ""Maximum number of results to return (default: 100)"",
      ""default"": 100
    },
    ""excludeDirs"": {
      ""type"": ""array"",
      ""description"": ""Directory names to exclude (e.g., ['bin', 'obj', 'node_modules'])"",
      ""items"": { ""type"": ""string"" }
    }
  },
  ""required"": [""path"", ""pattern""]
}";

    protected override Task<ToolResult> ExecuteInternalAsync(string parameters, CancellationToken cancellationToken)
    {
        var args = DeserializeParameters<SearchFilesArgs>(parameters);
        if (args == null || string.IsNullOrWhiteSpace(args.Path))
        {
            return Task.FromResult(ToolResult.Failure(Name, "Path parameter is required"));
        }

        if (string.IsNullOrWhiteSpace(args.Pattern))
        {
            return Task.FromResult(ToolResult.Failure(Name, "Pattern parameter is required"));
        }

        if (!Directory.Exists(args.Path))
        {
            return Task.FromResult(ToolResult.Failure(Name, $"Directory not found: {args.Path}"));
        }

        var excludeDirs = new HashSet<string>(args.ExcludeDirs ?? new List<string> { "bin", "obj", ".git", "node_modules" }, StringComparer.OrdinalIgnoreCase);
        var results = new List<string>();

        try
        {
            SearchDirectory(args.Path, args.Pattern, excludeDirs, results, args.MaxResults);
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Failure(Name, $"Search failed: {ex.Message}", ex));
        }

        var output = new System.Text.StringBuilder();
        output.AppendLine($"Search results for '{args.Pattern}' in {args.Path}:");
        output.AppendLine($"Found {results.Count} file(s):");
        output.AppendLine();

        foreach (var file in results)
        {
            var relativePath = Path.GetRelativePath(args.Path, file);
            var fileInfo = new FileInfo(file);
            output.AppendLine($"  📄 {relativePath} ({FormatFileSize(fileInfo.Length)})");
        }

        if (results.Count >= args.MaxResults)
        {
            output.AppendLine();
            output.AppendLine($"(Limited to {args.MaxResults} results. There may be more matches.)");
        }

        var metadata = new Dictionary<string, object>
        {
            ["matchCount"] = results.Count,
            ["searchPath"] = args.Path,
            ["pattern"] = args.Pattern
        };

        return Task.FromResult(ToolResult.CreateSuccess(Name, output.ToString(), metadata));
    }

    private void SearchDirectory(string path, string pattern, HashSet<string> excludeDirs, List<string> results, int maxResults)
    {
        if (results.Count >= maxResults)
            return;

        try
        {
            // Search files in current directory
            var files = Directory.GetFiles(path, pattern);
            foreach (var file in files)
            {
                results.Add(file);
                if (results.Count >= maxResults)
                    return;
            }

            // Recursively search subdirectories
            var directories = Directory.GetDirectories(path);
            foreach (var dir in directories)
            {
                var dirName = Path.GetFileName(dir);
                if (!excludeDirs.Contains(dirName))
                {
                    SearchDirectory(dir, pattern, excludeDirs, results, maxResults);
                }

                if (results.Count >= maxResults)
                    return;
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we don't have access to
        }
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

    private class SearchFilesArgs
    {
        public string Path { get; set; } = string.Empty;
        public string Pattern { get; set; } = "*.*";
        public int MaxResults { get; set; } = 100;
        public List<string>? ExcludeDirs { get; set; }
    }
}
