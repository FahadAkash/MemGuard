namespace MemGuard.Core.Models;

public class LabReport
{
    public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
    public IReadOnlyList<TypeSummary> TopTypesByRetainedSize { get; set; } = Array.Empty<TypeSummary>();
    public IReadOnlyList<TypeSummary> TopTypesByInstanceCount { get; set; } = Array.Empty<TypeSummary>();
    public IReadOnlyList<SegmentSummary> Segments { get; set; } = Array.Empty<SegmentSummary>();
    public IReadOnlyList<FeatureStatus> FeatureStatuses { get; set; } = Array.Empty<FeatureStatus>();
    public string Notes { get; set; } = string.Empty;
    public long TotalObjects { get; set; }
    public ulong TotalHeapSize
    {
        get; set;
    }
}