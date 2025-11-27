namespace MemGuard.Core.Interfaces;

public interface IObjectGraphAnalyzer
{
    void BuildObjectGraph(IEnumerable<ulong> rootObjects);
#pragma warning disable CA1002 // Do not expose generic lists

    List<ulong> GetBackReferences(ulong objectAddress, int maxDepth);
    List<ulong> GetBackReferences(ulong objectAddress);
#pragma warning restore CA1002 // Do not expose generic lists

    int GetObjectReferenceCount(ulong objectAddress);
#pragma warning disable CA1002 // Do not expose generic lists

    public List<List<ulong>> FindPathsToObject(ulong target, int maxPaths);
#pragma warning restore CA1002 // Do not expose generic lists

    List<List<ulong>> DetectCycles(int maxCycle = 5);

    Dictionary<ulong, long> CalculateRetainedSizes();
    void PrintObjectDetails(ulong objAddr);

}

