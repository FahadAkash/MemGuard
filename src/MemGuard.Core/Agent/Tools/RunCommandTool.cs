using System.Diagnostics;
using System.Text;

namespace MemGuard.Core.Agent.Tools;

/// <summary>
/// Tool for running shell commands
/// </summary>
public class RunCommandTool : AgentTool
{
    public override string Name => "run_command";
    public override string Description => "Execute a shell command and return the output. Use this to run dotnet commands, build projects, run tests, etc.";
    public override string Category => "Execution";
    public override string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""command"": {
      ""type"": ""string"",
      ""description"": ""The command to execute (e.g., 'dotnet build', 'dotnet test')""
    },
    ""workingDirectory"": {
      ""type"": ""string"",
      ""description"": ""Working directory for the command (default: current directory)""
    },
    ""timeoutSeconds"": {
      ""type"": ""integer"",
      ""description"": ""Timeout in seconds (default: 60)"",
      ""default"": 60
    }
  },
  ""required"": [""command""]
}";

    protected override async Task<ToolResult> ExecuteInternalAsync(string parameters, CancellationToken cancellationToken)
    {
        var args = DeserializeParameters<RunCommandArgs>(parameters);
        if (args == null || string.IsNullOrWhiteSpace(args.Command))
        {
            return ToolResult.Failure(Name, "Command parameter is required");
        }

        var workingDir = string.IsNullOrWhiteSpace(args.WorkingDirectory) 
            ? Directory.GetCurrentDirectory() 
            : args.WorkingDirectory;

        if (!Directory.Exists(workingDir))
        {
            return ToolResult.Failure(Name, $"Working directory not found: {workingDir}");
        }

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {args.Command}",
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var timeout = TimeSpan.FromSeconds(args.TimeoutSeconds);
            var completed = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds), cancellationToken);

            if (!completed)
            {
                process.Kill();
                return ToolResult.Failure(Name, $"Command timed out after {args.TimeoutSeconds} seconds");
            }

            var exitCode = process.ExitCode;
            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            var result = new StringBuilder();
            result.AppendLine($"Command: {args.Command}");
            result.AppendLine($"Working Directory: {workingDir}");
            result.AppendLine($"Exit Code: {exitCode}");
            result.AppendLine();
            
            if (!string.IsNullOrWhiteSpace(output))
            {
                result.AppendLine("Output:");
                result.AppendLine(output);
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                result.AppendLine("Errors:");
                result.AppendLine(error);
            }

            var metadata = new Dictionary<string, object>
            {
                ["exitCode"] = exitCode,
                ["workingDirectory"] = workingDir,
                ["command"] = args.Command
            };

            if (exitCode == 0)
            {
                return ToolResult.CreateSuccess(Name, result.ToString(), metadata);
            }
            else
            {
                return ToolResult.Failure(Name, result.ToString());
            }
        }
        catch (Exception ex)
        {
            return ToolResult.Failure(Name, $"Command execution failed: {ex.Message}", ex);
        }
    }

    private class RunCommandArgs
    {
        public string Command { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 60;
    }
}
