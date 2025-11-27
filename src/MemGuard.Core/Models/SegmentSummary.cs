namespace MemGuard.Core.Models;

public class SegmentSummary
{
    public int Generation { get; set; }
    public ulong SegmentStart { get; set; }
    public ulong SegmentLength { get; set; }
    public bool IsLargeObjectSegment { get; set; }
    public ulong FreeSpaceApprox { get; set; }

    public string FormattedLength => FormatBytes(SegmentLength);
    public string FormattedFreeSpace => FormatBytes(FreeSpaceApprox);
    public double FragmentationPercentage => SegmentLength > 0
        ? (double)FreeSpaceApprox / SegmentLength * 100
        : 0;

    private static string FormatBytes(ulong bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
