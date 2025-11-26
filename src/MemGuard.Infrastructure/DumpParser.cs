using Microsoft.Diagnostics.Runtime;
using System.Text.RegularExpressions;
using MemGuard.Core;

namespace MemGuard.Infrastructure;

/// <summary>
/// Parses memory dumps using ClrMD library
/// </summary>
public static class DumpParser
{
    /// <summary>
    /// Loads a memory dump for analysis
    /// </summary>
    /// <param name="filePath">Path to the dump file</param>
    /// <returns>ClrRuntime instance for analysis</returns>
    public static ClrRuntime LoadDump(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var dataTarget = DataTarget.LoadDump(filePath);
        return dataTarget.ClrVersions.Single().CreateRuntime();
    }
    
    /// <summary>
    /// Extracts heap statistics from the dump
    /// </summary>
    /// <param name="runtime">ClrRuntime instance</param>
    /// <returns>Heap diagnostic information</returns>
    public static HeapDiagnostic ExtractHeapInfo(ClrRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        var heap = runtime.Heap;
        long totalSize = 0;
        long freeSize = 0;
        
        foreach (var segment in heap.Segments)
        {
            totalSize += (long)segment.Length;
            // Note: In a real implementation, we'd extract actual free space information
            // This is simplified for demonstration purposes
        }
        
        var fragmentation = totalSize > 0 ? (double)freeSize / totalSize : 0;
        
        return new HeapDiagnostic(
            FragmentationLevel: fragmentation,
            LargestFreeBlock: 0, // Would be extracted in real implementation
            TotalSize: totalSize);
    }
    
    /// <summary>
    /// Detects deadlocks in the dump
    /// </summary>
    /// <param name="runtime">ClrRuntime instance</param>
    /// <returns>Deadlock diagnostic information</returns>
    public static DeadlockDiagnostic DetectDeadlocks(ClrRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        var threadIds = new List<int>();
        var lockObjects = new List<string>();
        
        // Simplified deadlock detection - in reality this would be more complex
        foreach (var thread in runtime.Threads.Where(t => t.IsAlive))
        {
            if (thread.LockCount > 5) // Arbitrary threshold
            {
                threadIds.Add(thread.ManagedThreadId);
                // Would extract actual lock object information in full implementation
                lockObjects.Add($"Thread {thread.ManagedThreadId} has {thread.LockCount} locks");
            }
        }
        
        return new DeadlockDiagnostic(threadIds, lockObjects);
    }
}

/// <summary>
/// Sanitizes memory dumps to remove PII
/// </summary>
public class DumpSanitizer
{
    private readonly Regex[] _piiPatterns = {
        new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b"), // Email
        new(@"\b\d{3}-\d{2}-\d{4}\b"), // SSN
        new(@"\b(?:\d{4}[ -]?){3}\d{4}\b"), // Credit card
        new(@"\b\d{3}-\d{3}-\d{4}\b") // Phone number
    };
    
    /// <summary>
    /// Removes PII from dump content
    /// </summary>
    /// <param name="content">Raw dump content</param>
    /// <returns>Sanitized content</returns>
    public string Sanitize(string content)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        foreach (var pattern in _piiPatterns)
        {
            content = pattern.Replace(content, "[REDACTED]");
        }
        return content;
    }
}