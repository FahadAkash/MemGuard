using System.Diagnostics;
using System.Text.Json;

namespace MemGuard.Core.Agent.Tools;

/// <summary>
/// Tool for killing processes by name or PID
/// </summary>
public class KillProcessTool : AgentTool
{
    public override string Name => "kill_process";
    public override string Description => "Kill/terminate processes by name or PID. Useful for cleaning up stuck processes before building/running.";
    public override string Category => "Process Management";
    public override string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""processName"": {
      ""type"": ""string"",
      ""description"": ""Name of the process to kill (e.g., 'LeakyApp', 'notepad')""
    },
    ""pid"": {
      ""type"": ""integer"",
      ""description"": ""Process ID to kill (alternative to processName)""
    },
    ""force"": {
      ""type"": ""boolean"",
      ""description"": ""Force kill the process (default: true)"",
      ""default"": true
    }
  }
}";

    protected override Task<ToolResult> ExecuteInternalAsync(string parameters, CancellationToken cancellationToken)
    {
        var args = DeserializeParameters<KillProcessArgs>(parameters);
        if (args == null)
        {
            return Task.FromResult(ToolResult.Failure(Name, "Invalid parameters"));
        }

        var output = new System.Text.StringBuilder();
        var killedCount = 0;

        try
        {
            Process[] processes;

            if (args.Pid.HasValue)
            {
                // Kill by PID
                try
                {
                    var process = Process.GetProcessById(args.Pid.Value);
                    process.Kill(args.Force);
                    output.AppendLine($"✓ Killed process {process.ProcessName} (PID: {args.Pid})");
                    killedCount = 1;
                }
                catch (ArgumentException)
                {
                    return Task.FromResult(ToolResult.Failure(Name, $"Process with PID {args.Pid} not found"));
                }
            }
            else if (!string.IsNullOrWhiteSpace(args.ProcessName))
            {
                // Kill by name
                var processName = args.ProcessName.Replace(".exe", ""); // Remove .exe if present
                processes = Process.GetProcessesByName(processName);

                if (processes.Length == 0)
                {
                    return Task.FromResult(ToolResult.Failure(Name, $"No processes found with name '{processName}'"));
                }

                output.AppendLine($"Found {processes.Length} process(es) named '{processName}':");
                
                foreach (var process in processes)
                {
                    try
                    {
                        output.AppendLine($"  Killing PID {process.Id}...");
                        process.Kill(args.Force);
                        killedCount++;
                    }
                    catch (Exception ex)
                    {
                        output.AppendLine($"  ✗ Failed to kill PID {process.Id}: {ex.Message}");
                    }
                }

                output.AppendLine($"\n✓ Successfully killed {killedCount}/{processes.Length} process(es)");
            }
            else
            {
                return Task.FromResult(ToolResult.Failure(Name, "Either processName or pid must be specified"));
            }

            var metadata = new Dictionary<string, object>
            {
                ["killedCount"] = killedCount
            };

            return Task.FromResult(ToolResult.CreateSuccess(Name, output.ToString(), metadata));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Failure(Name, $"Failed to kill process: {ex.Message}", ex));
        }
    }

    private class KillProcessArgs
    {
        public string? ProcessName { get; set; }
        public int? Pid { get; set; }
        public bool Force { get; set; } = true;
    }
}
