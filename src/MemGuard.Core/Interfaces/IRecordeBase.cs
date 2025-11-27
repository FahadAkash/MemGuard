namespace MemGuard.Core.Interfaces;

public interface IRecordeBase
{
    AnalysisResult ExecuteAnalysis(AnalysisOptions options, CancellationToken ct = default);
    void LoadDumpFile(string dumpPath);

}

