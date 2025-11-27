using MemGuard.Core;
using MemGuard.Core.Interfaces;
using Microsoft.Diagnostics.Runtime;

namespace MemGuard.Infrastructure.Extractors;

public class HeapExtractor : IDiagnosticExtractor
{
    public string Name => "Heap";

    public Task<DiagnosticBase?> ExtractAsync(ClrRuntime runtime, CancellationToken cancellationToken = default)
    {
        if (!runtime.Heap.CanWalkHeap)
            return Task.FromResult<DiagnosticBase?>(null);

        long totalSize = 0;
        long freeSize = 0;
        long largestFree = 0;

        foreach (var segment in runtime.Heap.Segments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            totalSize += (long)segment.Length;
            
            // Simplified free space check (ClrMD doesn't make this trivial without walking)
            // In a real scenario, we'd walk the free list
        }

        // Simulate fragmentation calculation for now as full walk is expensive
        // In production, we would use runtime.Heap.EnumerateObjects() and check for Free objects
        
        var fragmentation = totalSize > 0 ? 0.15 : 0; // Mock 15% fragmentation for demo

        return Task.FromResult<DiagnosticBase?>(new HeapDiagnostic(
            FragmentationLevel: fragmentation,
            LargestFreeBlock: largestFree,
            TotalSize: totalSize));
    }
}
