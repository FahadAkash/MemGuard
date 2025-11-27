namespace MemGuard.Core.Models;

public class TypeSummary
{
    public string TypeName { get; set; } = string.Empty;
    public ulong InstanceCount { get; set; }
    public ulong ShallowSize { get; set; }
    public ulong RetainedSize { get; set; }
    public IReadOnlyList<ulong> ExampleObjectAddresses { get; set; } = Array.Empty<ulong>();
    public string FormattedShallowSize => FormatBytes(ShallowSize);
    public string FormattedRetainedSize => FormatBytes(RetainedSize);
    private static string FormatBytes(ulong bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
