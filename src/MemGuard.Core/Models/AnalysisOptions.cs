namespace MemGuard.Core.Models;

public class AnalysisOptions
{
    public int TopN { get; set; } = 25;
    public bool CalculateAccurateRetainedSize { get; set; } = true;
    public int MaxSamplesPerType { get; set; } = 5;
    public int AccurateRetainedSizeTopCount { get; set; } = 10;
}
