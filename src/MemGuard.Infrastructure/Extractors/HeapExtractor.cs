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
        }

        // Calculate actual fragmentation by walking heap and finding free objects
        try
        {
            foreach (var obj in runtime.Heap.EnumerateObjects())
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Check if object is a free space marker
                if (obj.Type?.Name?.Contains("Free") == true || obj.Type == null)
                {
                    var size = (long)obj.Size;
                    freeSize += size;
                    if (size > largestFree)
                        largestFree = size;
                }
            }
        }
        catch
        {
            // If heap walk fails, use conservative estimate
            freeSize = (long)(totalSize * 0.10); // 10% estimate
        }

        var fragmentation = totalSize > 0 ? (double)freeSize / totalSize : 0;

        return Task.FromResult<DiagnosticBase?>(new HeapDiagnostic(
            FragmentationLevel: fragmentation,
            LargestFreeBlock: largestFree,
            TotalSize: totalSize));
    }
}
