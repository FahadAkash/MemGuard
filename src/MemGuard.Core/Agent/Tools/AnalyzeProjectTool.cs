using MemGuard.Core.Interfaces;
using System.Text.Json;

namespace MemGuard.Core.Agent.Tools;

/// <summary>
/// Tool for analyzing project structure and files
/// </summary>
public class AnalyzeProjectTool : AgentTool
{
    private readonly IFileManager _fileManager;

    public AnalyzeProjectTool(IFileManager fileManager)
    {
        _fileManager = fileManager;
    }

    public override string Name => "analyze_project";
    public override string Description => "Analyze a .NET project structure, find C# files, and provide an overview of the codebase.";
    public override string Category => "Code Analysis";
    public override string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""path"": {
      ""type"": ""string"",
      ""description"": ""Path to the project directory""
    },
    ""includeFileContents"": {
      ""type"": ""boolean"",
      ""description"": ""Whether to include file contents in analysis (default: false)"",
      ""default"": false
    },
    ""maxFileSizeKb"": {
      ""type"": ""integer"",
      ""description"": ""Maximum file size to include in KB (default: 100)"",
      ""default"": 100
    }
  },
  ""required"": [""path""]
}";

    protected override async Task<ToolResult> ExecuteInternalAsync(string parameters, CancellationToken cancellationToken)
    {
        var args = DeserializeParameters<AnalyzeProjectArgs>(parameters);
        if (args == null || string.IsNullOrWhiteSpace(args.Path))
        {
            return ToolResult.Failure(Name, "Path parameter is required");
        }

        if (!Directory.Exists(args.Path))
        {
            return ToolResult.Failure(Name, $"Project directory not found: {args.Path}");
        }

        var structure = await _fileManager.GetProjectStructureAsync(args.Path);
        var output = new System.Text.StringBuilder();
        
        output.AppendLine($"Project Analysis: {args.Path}");
        output.AppendLine();
        output.AppendLine("Project Structure:");
        output.AppendLine(JsonSerializer.Serialize(structure, new JsonSerializerOptions { WriteIndented = true }));
        output.AppendLine();

        // Find C# files
        var csFiles = Directory.GetFiles(args.Path, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
            .ToList();

        output.AppendLine($"C# Files Found: {csFiles.Count}");
        output.AppendLine();

        // Count classes, interfaces, etc. (simple heuristic)
        int classCount = 0;
        int interfaceCount = 0;
        int totalLines = 0;

        foreach (var file in csFiles)
        {
            var content = await File.ReadAllTextAsync(file, cancellationToken);
            totalLines += content.Split('\n').Length;
            classCount += System.Text.RegularExpressions.Regex.Matches(content, @"\bclass\s+\w+").Count;
            interfaceCount += System.Text.RegularExpressions.Regex.Matches(content, @"\binterface\s+\w+").Count;
        }

        output.AppendLine("Code Statistics:");
        output.AppendLine($"  Total Files: {csFiles.Count}");
        output.AppendLine($"  Total Lines: {totalLines:N0}");
        output.AppendLine($"  Classes: {classCount}");
        output.AppendLine($"  Interfaces: {interfaceCount}");
        output.AppendLine();

        // List key files
        output.AppendLine("Key Files:");
        foreach (var file in csFiles.Take(20))
        {
            var relativePath = Path.GetRelativePath(args.Path, file);
            var fileInfo = new FileInfo(file);
            output.AppendLine($"  📄 {relativePath} ({fileInfo.Length} bytes)");
        }

        if (csFiles.Count > 20)
        {
            output.AppendLine($"  ... and {csFiles.Count - 20} more files");
        }

        var metadata = new Dictionary<string, object>
        {
            ["fileCount"] = csFiles.Count,
            ["totalLines"] = totalLines,
            ["classCount"] = classCount,
            ["interfaceCount"] = interfaceCount
        };

        return ToolResult.CreateSuccess(Name, output.ToString(), metadata);
    }

    private class AnalyzeProjectArgs
    {
        public string Path { get; set; } = string.Empty;
        public bool IncludeFileContents { get; set; } = false;
        public int MaxFileSizeKb { get; set; } = 100;
    }
}
