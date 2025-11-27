using MemGuard.Core;
using MemGuard.Core.Interfaces;
using Microsoft.Diagnostics.Runtime;

namespace MemGuard.Infrastructure.Extractors;

public class DeadlockExtractor : IDiagnosticExtractor
{
    public string Name => "Deadlock";

    public Task<DiagnosticBase?> ExtractAsync(ClrRuntime runtime, CancellationToken cancellationToken = default)
    {
        var threadIds = new List<int>();
        var lockObjects = new List<string>();

        // In ClrMD 3.0, we need to check for threads that are waiting
        // We'll look for threads that are blocked or waiting on synchronization
        foreach (var thread in runtime.Threads)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check if thread is blocked (not running)
            // Threads with high lock count or in wait state are potential deadlock candidates
            if (thread.LockCount > 0)
            {
                threadIds.Add(thread.ManagedThreadId);
                
                // Get more details about what the thread is doing
                var stackTrace = string.Join(" -> ", thread.EnumerateStackTrace().Take(3).Select(f => f.Method?.Name ?? "Unknown"));
                lockObjects.Add($"Thread {thread.ManagedThreadId} (Locks: {thread.LockCount}) - {stackTrace}");
            }
        }

        if (threadIds.Count > 0)
        {
            return Task.FromResult<DiagnosticBase?>(new DeadlockDiagnostic(
                ThreadIds: threadIds.Distinct().ToList(),
                LockObjects: lockObjects));
        }

        return Task.FromResult<DiagnosticBase?>(null);
    }
}
