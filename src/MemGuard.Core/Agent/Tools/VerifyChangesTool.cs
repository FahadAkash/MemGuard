using System.Text.Json;
using MemGuard.Core.Services;

namespace MemGuard.Core.Agent.Tools;

public class VerifyChangesTool : AgentTool
{
    public override string Name => "verify_changes";
    public override string Description => "Verifies code changes by running build or tests. Use this after making code modifications to ensure correctness.";
    public override string Category => "Verification";

    public override string ParametersSchema => JsonSerializer.Serialize(new
    {
        type = "object",
        properties = new
        {
            type = new
            {
                type = "string",
                description = "Type of verification to run: 'build' or 'test'",
                @enum = new[] { "build", "test" }
            },
            project_path = new
            {
                type = "string",
                description = "Optional path to specific project file or directory"
            }
        },
        required = new[] { "type" }
    });

    private class VerifyArgs
    {
        public string Type { get; set; } = "build";
        public string? ProjectPath { get; set; }
    }

    protected override async Task<ToolResult> ExecuteInternalAsync(string parameters, CancellationToken cancellationToken)
    {
        try
        {
            var args = JsonSerializer.Deserialize<VerifyArgs>(parameters, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (args == null) return ToolResult.Failure(Name, "Invalid parameters");

            var workingDir = Environment.CurrentDirectory;
            
            // Determine working directory from project path
            if (!string.IsNullOrEmpty(args.ProjectPath))
            {
                if (File.Exists(args.ProjectPath))
                {
                    // If it's a file (e.g., .csproj), use its directory
                    workingDir = Path.GetDirectoryName(args.ProjectPath) ?? Environment.CurrentDirectory;
                }
                else if (Directory.Exists(args.ProjectPath))
                {
                    // If it's a directory, use it directly
                    workingDir = args.ProjectPath;
                }
            }

            var engine = new VerificationEngine(workingDir);
            VerificationResult result;

            if (args.Type.ToLower() == "test")
            {
                result = await engine.VerifyTestsAsync(args.ProjectPath);
            }
            else
            {
                result = await engine.VerifyBuildAsync(args.ProjectPath);
            }

            if (result.Success)
            {
                return ToolResult.CreateSuccess(Name, 
                    $"Verification ({args.Type}) PASSED in {result.Duration.TotalSeconds:F1}s\n\nOutput:\n{result.Output}");
            }
            else
            {
                var errorSummary = string.Join("\n", result.Errors.Take(5));
                return ToolResult.Failure(Name, 
                    $"Verification ({args.Type}) FAILED in {result.Duration.TotalSeconds:F1}s\n\nErrors:\n{errorSummary}\n\nFull Output (truncated):\n{result.Output.Substring(0, Math.Min(result.Output.Length, 1000))}...");
            }
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(Name, $"Verification tool error: {ex.Message}");
        }
    }
}
