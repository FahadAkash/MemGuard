using MemGuard.Core.Interfaces;

namespace MemGuard.Core.Services;

public class ReportDisplayService : IReportDisplayService
{
    public void DisplayHeader(string title)
    {
        if (string.IsNullOrEmpty(title))
            throw new ArgumentNullException(nameof(title));

        Console.WriteLine(new string('=', 60));
        Console.WriteLine(title.ToUpper());
        Console.WriteLine(new string('=', 60));
        Console.WriteLine();
    }

    public void DisplayInfo(string message)
    {
        Console.WriteLine($"[INFO] {message}");
    }

    public void DisplaySuccess(string message)
    {
        Console.WriteLine($"[SUCCESS] {message}");
    }

    public void DisplayError(string message, Exception? ex = null)
    {
        Console.WriteLine($"[ERROR] {message}");
        if (ex != null)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
        Console.WriteLine();
    }

    public void DisplayReport(LeakReport report)
    {
        if (report == null)
            throw new ArgumentNullException(nameof(report));

        Console.WriteLine("MEMORY LEAK ANALYSIS REPORT");
        Console.WriteLine("==========================");
        Console.WriteLine($"Total Heap Size: {FormatBytes(report.TotalHeapSize)}");
        Console.WriteLine($"Total Objects: {report.TotalObjects:N0}");
        Console.WriteLine();

        Console.WriteLine("TOP TYPES BY RETAINED SIZE:");
        Console.WriteLine(new string('-', 40));
        foreach (var typeSummary in report.TopTypesByRetainedSize.Take(20))
        {
            Console.WriteLine($"{typeSummary.TypeName}:");
            Console.WriteLine($"  Retained Size: {FormatBytes(typeSummary.RetainedSize)}");
            Console.WriteLine($"  Instances: {typeSummary.InstanceCount:N0}");
            Console.WriteLine($"  Sample Addresses: [{string.Join(", ", typeSummary.ExampleObjectAddresses.Take(3).Select(a => $"0x{a:X}"))}]");
            Console.WriteLine();
        }
    }

    public void DisplayObjectInfo(ObjectInfo objInfo)
    {
        if (objInfo == null)
            throw new ArgumentNullException(nameof(objInfo));

        Console.WriteLine("OBJECT INFORMATION:");
        Console.WriteLine(new string('-', 25));
        Console.WriteLine($"Address: 0x{objInfo.Address:X}");
        Console.WriteLine($"Type: {objInfo.TypeName}");
        Console.WriteLine($"Size: {FormatBytes(objInfo.Size)}");
        Console.WriteLine($"Generation: {objInfo.Generation}");
        Console.WriteLine();
    }

    public void DisplayRetentionPath(RetentionPath path)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));

        Console.WriteLine("RETENTION PATH:");
        Console.WriteLine(new string('-', 20));
        Console.WriteLine($"Object Address: 0x{path.ObjectAddress:X}");
        Console.WriteLine("Path from root:");
        
        for (int i = 0; i < path.PathFromRoot.Count; i++)
        {
            Console.WriteLine($"{new string(' ', i * 2)}→ {path.PathFromRoot[i]}");
        }
        Console.WriteLine();
    }

    public string FormatBytes(ulong bytes)
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