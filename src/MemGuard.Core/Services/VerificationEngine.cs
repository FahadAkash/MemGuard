using System.Diagnostics;
using System.Text;
using MemGuard.Core.Agent;

namespace MemGuard.Core.Services;

public class VerificationResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Engine for verifying code changes through build and test execution
/// </summary>
public class VerificationEngine
{
    private readonly string _workingDirectory;

    public VerificationEngine(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    /// <summary>
    /// Verifies the project by running a build
    /// </summary>
    public async Task<VerificationResult> VerifyBuildAsync(string? projectPath = null)
    {
        return await RunDotnetCommandAsync("build", projectPath);
    }

    /// <summary>
    /// Verifies the project by running tests
    /// </summary>
    public async Task<VerificationResult> VerifyTestsAsync(string? projectPath = null)
    {
        return await RunDotnetCommandAsync("test", projectPath);
    }

    private async Task<VerificationResult> RunDotnetCommandAsync(string command, string? projectPath)
    {
        var sw = Stopwatch.StartNew();
        var result = new VerificationResult();
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = string.IsNullOrEmpty(projectPath) ? command : $"{command} \"{projectPath}\"",
                WorkingDirectory = _workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            await process.WaitForExitAsync();

            result.Success = process.ExitCode == 0;
            result.Output = outputBuilder.ToString();
            
            var errors = errorBuilder.ToString();
            if (!string.IsNullOrWhiteSpace(errors))
            {
                result.Errors.Add(errors);
            }

            // Parse build output for specific errors if failed
            if (!result.Success)
            {
                ParseBuildErrors(result.Output, result.Errors);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Execution failed: {ex.Message}");
        }
        finally
        {
            sw.Stop();
            result.Duration = sw.Elapsed;
        }

        return result;
    }

    private void ParseBuildErrors(string output, List<string> errors)
    {
        // Simple parser for dotnet build errors (format: file(line,col): error CODE: Message)
        var lines = output.Split(Environment.NewLine);
        foreach (var line in lines)
        {
            if (line.Contains(": error "))
            {
                errors.Add(line.Trim());
            }
        }
    }
}
