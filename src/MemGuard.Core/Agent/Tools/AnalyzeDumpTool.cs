using MemGuard.Core.Interfaces;

namespace MemGuard.Core.Agent.Tools;

/// <summary>
/// Tool for analyzing memory dumps
/// </summary>
public class AnalyzeDumpTool : AgentTool
{
    private readonly IDumpParser _dumpParser;

    public AnalyzeDumpTool(IDumpParser dumpParser)
    {
        _dumpParser = dumpParser;
    }

    public override string Name => "analyze_dump";
    public override string Description => "Analyze a memory dump file and extract diagnostic information about heap, threads, and potential memory issues.";
    public override string Category => "Code Analysis";
    public override string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""path"": {
      ""type"": ""string"",
      ""description"": ""Path to the .dmp file""
    },
    ""detailed"": {
      ""type"": ""boolean"",
      ""description"": ""Include detailed analysis (default: true)"",
      ""default"": true
    }
  },
  ""required"": [""path""]
}";

    protected override Task<ToolResult> ExecuteInternalAsync(string parameters, CancellationToken cancellationToken)
    {
        var args = DeserializeParameters<AnalyzeDumpArgs>(parameters);
        if (args == null || string.IsNullOrWhiteSpace(args.Path))
        {
            return Task.FromResult(ToolResult.Failure(Name, "Path parameter is required"));
        }

        if (!File.Exists(args.Path))
        {
            return Task.FromResult(ToolResult.Failure(Name, $"Dump file not found: {args.Path}"));
        }

        try
        {
            using var runtime = _dumpParser.LoadDump(args.Path);
            
            var output = new System.Text.StringBuilder();
            output.AppendLine($"Dump Analysis: {Path.GetFileName(args.Path)}");
            output.AppendLine();
            
            // Heap information
            output.AppendLine("Heap Information:");
            output.AppendLine($"  Segments: {runtime.Heap.Segments.Count()}");
            output.AppendLine($"  Total Size: {FormatBytes(runtime.Heap.Segments.Sum(s => (long)s.Length))}");
            output.AppendLine($"  Can Walk Heap: {runtime.Heap.CanWalkHeap}");
            output.AppendLine();

            // Thread information
            output.AppendLine("Thread Information:");
            output.AppendLine($"  Thread Count: {runtime.Threads.Count()}");
            output.AppendLine();

            if (args.Detailed)
            {
                // Sample objects from heap
                if (runtime.Heap.CanWalkHeap)
                {
                    output.AppendLine("Sample Objects (first 10 types):");
                    var typeStats = new Dictionary<string, int>();
                    
                    foreach (var obj in runtime.Heap.EnumerateObjects().Take(10000))
                    {
                        var typeName = obj.Type?.Name ?? "Unknown";
                        typeStats[typeName] = typeStats.GetValueOrDefault(typeName, 0) + 1;
                    }

                    foreach (var kvp in typeStats.OrderByDescending(x => x.Value).Take(10))
                    {
                        output.AppendLine($"  {kvp.Key}: {kvp.Value} instances");
                    }
                }
            }

            var fileInfo = new FileInfo(args.Path);
            var metadata = new Dictionary<string, object>
            {
                ["dumpSize"] = fileInfo.Length,
                ["heapSegments"] = runtime.Heap.Segments.Count(),
                ["threadCount"] = runtime.Threads.Count()
            };

            return Task.FromResult(ToolResult.CreateSuccess(Name, output.ToString(), metadata));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Failure(Name, $"Failed to analyze dump: {ex.Message}", ex));
        }
    }

    private static string FormatBytes(long bytes)
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

    private class AnalyzeDumpArgs
    {
        public string Path { get; set; } = string.Empty;
        public bool Detailed { get; set; } = true;
    }
}
