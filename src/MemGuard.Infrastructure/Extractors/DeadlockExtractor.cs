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

        foreach (var thread in runtime.Threads)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Simplified detection using LockCount as BlockingObjects API varies by ClrMD version
            if (thread.LockCount > 0)
            {
                threadIds.Add(thread.ManagedThreadId);
                lockObjects.Add($"Thread {thread.ManagedThreadId} has {thread.LockCount} locks");
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
