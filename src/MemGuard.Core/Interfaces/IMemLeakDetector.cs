using MemGuard.Core;

namespace MemGuard.Core.Interfaces;

public interface IMemLeakDetector : IDisposable, IRecordeBase
{

    void AttachToProcess(int processId);
    LeakReport AnalyzeHeap(AnalysisOptions options, CancellationToken cancellationToken = default);
    RetentionPath? FindRetentionPath(ulong objectAddress, CancellationToken cancellationToken = default);
    ObjectInfo? GetObjectInfo(ulong objectAddress);
    bool IsObjectAlive(ulong objectAddress);
     

}