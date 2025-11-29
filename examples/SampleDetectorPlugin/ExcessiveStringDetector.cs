using MemGuard.Core;
using MemGuard.Plugins;
using Microsoft.Diagnostics.Runtime;

namespace SampleDetectorPlugin;

/// <summary>
/// Sample detector plugin that demonstrates how to create custom detectors for MemGuard
/// This example detects excessive string allocations which might indicate a memory leak
/// </summary>
public class ExcessiveStringDetector : IDetectorPlugin
{
    public string Name => "ExcessiveStringDetector";
    public string Version => "1.0.0";
    public string Description => "Detects excessive string allocations that might indicate a memory leak";

    /// <summary>
    /// Analyze the memory dump for excessive string allocations
    /// </summary>
    public async Task<DiagnosticBase?> AnalyzeAsync(ClrRuntime runtime, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Count total string objects
                var stringCount = 0;
                var totalStringSize = 0L;
                var sampleStrings = new List<string>();

                foreach (var obj in runtime.Heap.EnumerateObjects())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (obj.Type?.Name == "System.String")
                    {
                        stringCount++;
                        totalStringSize += (long)obj.Size;

                        // Collect samples for evidence
                        if (sampleStrings.Count < 5)
                        {
                            try
                            {
                                var str = obj.AsString();
                                if (!string.IsNullOrEmpty(str) && str.Length < 100)
                                {
                                    sampleStrings.Add(str);
                                }
                            }
                            catch
                            {
                                // Ignore individual read failures
                            }
                        }
                    }

                    // Early exit if we have enough data
                    if (stringCount > 100000)
                        break;
                }

                // Determine if this is excessive (threshold: >50k strings or >50MB)
                var isExcessive = stringCount > 50000 || totalStringSize > 50 * 1024 * 1024;

                if (isExcessive)
                {
                    // Create a custom diagnostic (you can create your own diagnostic types)
                    // For this example, we'll return a generic diagnostic using PinnedObjectDiagnostic as template
                    return new PinnedObjectDiagnostic(
                        stringCount,
                        $"Excessive - {FormatBytes(totalStringSize)} total"
                    );
                }

                return null; // No issue detected
            }
            catch (Exception ex)
            {
                // Log error but don't fail the entire analysis
                Console.WriteLine($"[ExcessiveStringDetector] Error: {ex.Message}");
                return null;
            }
        }, cancellationToken);
    }

    private string FormatBytes(long bytes)
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
}
