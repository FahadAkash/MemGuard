using MemGuard.Core.Interfaces;
using System.Collections.Immutable;

namespace MemGuard.Core.Services;

/// <summary>
/// Service for performing complete memory leak analysis workflows
/// Orchestrates the leak detector and display services
/// </summary>
public class MemoryAnalysisService : IMemoryAnalysisService, IDisposable
{
    private readonly IMemLeakDetector _detector;
    private readonly IReportDisplayService _displayService;

    public MemoryAnalysisService(IMemLeakDetector detector, IReportDisplayService displayService)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
    }

    public MemoryAnalysisService()
    {
        _detector = new MemLeakDetectorService();
        _displayService = new ReportDisplayService();
    }

    #region Dump Analysis

    public void AnalyzeDumpFile(string dumpPath, AnalysisOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _displayService.DisplayHeader("Memory Leak Analysis");
            _displayService.DisplayInfo($"Loading dump file: {dumpPath}");

            // Load dump
            _detector.LoadDumpFile(dumpPath);
            _displayService.DisplaySuccess("Dump loaded successfully\n");

            // Configure options
            options ??= new AnalysisOptions
            {
                TopN = 30,
                CalculateAccurateRetainedSize = true,
                MaxSamplesPerType = 5,
                AccurateRetainedSizeTopCount = 10
            };

            // Analyze heap
            _displayService.DisplayInfo("Analyzing heap...");
            var report = _detector.AnalyzeHeap(options, cancellationToken);
            _displayService.DisplaySuccess("Analysis complete\n");

            // Display report
            _displayService.DisplayReport(report);

            // Analyze first suspicious object
            AnalyzeFirstSuspiciousObject(report, cancellationToken);
        }
        catch (Exception ex)
        {
            _displayService.DisplayError("Failed to analyze dump file", ex);
        }
    }

    public void AnalyzeLiveProcess(int processId, AnalysisOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _displayService.DisplayHeader("Live Process Analysis");
            _displayService.DisplayInfo($"Attaching to process ID: {processId}");

            // Attach to process
            _detector.AttachToProcess(processId);
            _displayService.DisplaySuccess("Attached successfully\n");

            // Configure options
            options ??= new AnalysisOptions { TopN = 30 };

            // Analyze heap
            _displayService.DisplayInfo("Analyzing heap...");
            var report = _detector.AnalyzeHeap(options, cancellationToken);
            _displayService.DisplaySuccess("Analysis complete\n");

            // Display report
            _displayService.DisplayReport(report);
        }
        catch (Exception ex)
        {
            _displayService.DisplayError("Failed to analyze live process", ex);
        }
    }

    #endregion

    #region Object Analysis

    public void FindObjectRetentionPath(string dumpPath, ulong objectAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            _displayService.DisplayInfo($"Loading dump: {dumpPath}");
            _detector.LoadDumpFile(dumpPath);
            _displayService.DisplaySuccess("Dump loaded\n");

            _displayService.DisplayInfo($"Finding retention path for object 0x{objectAddress:X}...");
            var path = _detector.FindRetentionPath(objectAddress, cancellationToken);

            if (path != null)
            {
                _displayService.DisplayRetentionPath(path);
            }
            else
            {
                _displayService.DisplayInfo("No retention path found for the object.");
            }
        }
        catch (Exception ex)
        {
            _displayService.DisplayError("Failed to find retention path", ex);
        }
    }

    public void AnalyzeSpecificObject(string dumpPath, ulong objectAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            _displayService.DisplayHeader("Object Analysis");
            _displayService.DisplayInfo($"Loading dump: {dumpPath}");
            _detector.LoadDumpFile(dumpPath);
            _displayService.DisplaySuccess("Dump loaded\n");

            // Get object info
            var objInfo = _detector.GetObjectInfo(objectAddress);
            if (objInfo == null)
            {
                _displayService.DisplayError($"Object not found at address 0x{objectAddress:X}");
                return;
            }

            // Display object details
            _displayService.DisplayObjectInfo(objInfo);

            // Check if alive
            var isAlive = _detector.IsObjectAlive(objectAddress);
            _displayService.DisplayInfo($"Object is {(isAlive ? "ALIVE (reachable from roots)" : "DEAD (unreachable)")}");

            // Find retention path if alive
            if (isAlive)
            {
                _displayService.DisplayInfo("\nFinding retention path...");
                var path = _detector.FindRetentionPath(objectAddress, cancellationToken);
                if (path != null)
                {
                    _displayService.DisplayRetentionPath(path);
                }
            }
        }
        catch (Exception ex)
        {
            _displayService.DisplayError("Failed to analyze object", ex);
        }
    }

    #endregion

    #region Quick and Deep Analysis

    public void QuickAnalysis(string dumpPath, CancellationToken cancellationToken = default)
    {
        var options = new AnalysisOptions
        {
            TopN = 10,
            CalculateAccurateRetainedSize = false,
            MaxSamplesPerType = 1,
            AccurateRetainedSizeTopCount = 0
        };

        _displayService.DisplayHeader("Quick Analysis Mode");
        AnalyzeDumpFile(dumpPath, options, cancellationToken);
    }

    public void DeepAnalysis(string dumpPath, CancellationToken cancellationToken = default)
    {
        var options = new AnalysisOptions
        {
            TopN = 50,
            CalculateAccurateRetainedSize = true,
            MaxSamplesPerType = 10,
            AccurateRetainedSizeTopCount = 20
        };

        _displayService.DisplayHeader("Deep Analysis Mode");
        AnalyzeDumpFile(dumpPath, options, cancellationToken);
    }

    #endregion

    #region Dump Comparison

    public void CompareDumps(string dumpPath1, string dumpPath2, CancellationToken cancellationToken = default)
    {
        try
        {
            _displayService.DisplayHeader("Dump Comparison");

            // Analyze first dump
            _displayService.DisplayInfo("Analyzing first dump...");
            _detector.LoadDumpFile(dumpPath1);
            var report1 = _detector.AnalyzeHeap(new AnalysisOptions { TopN = 30 }, cancellationToken);
            _displayService.DisplaySuccess("First dump analyzed\n");

            // Analyze second dump
            _displayService.DisplayInfo("Analyzing second dump...");
            using var detector2 = new MemLeakDetectorService();
            detector2.LoadDumpFile(dumpPath2);
            var report2 = detector2.AnalyzeHeap(new AnalysisOptions { TopN = 30 }, cancellationToken);
            _displayService.DisplaySuccess("Second dump analyzed\n");

            // Display comparison
            DisplayComparison(report1, report2);
        }
        catch (Exception ex)
        {
            _displayService.DisplayError("Failed to compare dumps", ex);
        }
    }

    #endregion

    #region Private Helper Methods

    private void AnalyzeFirstSuspiciousObject(LeakReport report, CancellationToken cancellationToken)
    {
        if (report.TopTypesByRetainedSize.Count == 0)
            return;

        var firstType = report.TopTypesByRetainedSize[0];
        if (firstType.ExampleObjectAddresses.Count == 0)
            return;

        var objAddress = firstType.ExampleObjectAddresses[0];

        try
        {
            _displayService.DisplayInfo($"\nAnalyzing sample object from top type: {firstType.TypeName}");
            _displayService.DisplayInfo($"Object address: 0x{objAddress:X}");

            // Check if alive
            var isAlive = _detector.IsObjectAlive(objAddress);
            _displayService.DisplayInfo($"Object status: {(isAlive ? "ALIVE" : "DEAD")}");

            // Get object info
            var objInfo = _detector.GetObjectInfo(objAddress);
            if (objInfo != null)
            {
                Console.WriteLine($"  Type: {objInfo.TypeName}");
                Console.WriteLine($"  Size: {_displayService.FormatBytes(objInfo.Size)} ({objInfo.Size:N0} bytes)");
                Console.WriteLine($"  Generation: {objInfo.Generation}");
            }

            // Find retention path
            if (isAlive)
            {
                _displayService.DisplayInfo($"\nFinding retention path for 0x{objAddress:X}...");
                var path = _detector.FindRetentionPath(objAddress, cancellationToken);
                
                if (path != null)
                {
                    Console.WriteLine("\nRetention path (why object is kept alive):");
                    for (int i = 0; i < path.PathFromRoot.Count; i++)
                    {
                        Console.WriteLine($"  {new string(' ', i * 2)}→ {path.PathFromRoot[i]}");
                    }
                }
                else
                {
                    _displayService.DisplayInfo("Object is not reachable from any root");
                }
            }

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            _displayService.DisplayError("Failed to analyze sample object", ex);
        }
    }

    private void DisplayComparison(LeakReport report1, LeakReport report2)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  DUMP COMPARISON REPORT                    ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        // Overall comparison
        Console.WriteLine("OVERALL CHANGES:");
        Console.WriteLine($"  Objects:   {report1.TotalObjects:N0} → {report2.TotalObjects:N0} ({GetChangeIndicator((long)report1.TotalObjects, (long)report2.TotalObjects)})");
        Console.WriteLine($"  Heap Size: {_displayService.FormatBytes(report1.TotalHeapSize)} → {_displayService.FormatBytes(report2.TotalHeapSize)} ({GetChangeIndicator((long)report1.TotalHeapSize, (long)report2.TotalHeapSize)})");
        Console.WriteLine();

        // Type comparison
        Console.WriteLine("TOP TYPES WITH MOST GROWTH:");
        Console.WriteLine($"{"Type Name",-50} {"Change",15}");
        Console.WriteLine(new string('─', 67));

        var typeMap1 = report1.TopTypesByRetainedSize.ToDictionary(t => t.TypeName, t => t.RetainedSize);
        var typeMap2 = report2.TopTypesByRetainedSize.ToDictionary(t => t.TypeName, t => t.RetainedSize);

        var allTypes = typeMap1.Keys.Union(typeMap2.Keys);
        var growthList = allTypes
            .Select(type =>
            {
                var size1 = typeMap1.ContainsKey(type) ? typeMap1[type] : 0;
                var size2 = typeMap2.ContainsKey(type) ? typeMap2[type] : 0;
                var growth = (long)size2 - (long)size1;
                return new { Type = type, Growth = growth, Size2 = size2 };
            })
            .OrderByDescending(x => x.Growth)
            .Take(15);

        foreach (var item in growthList)
        {
            var shortName = item.Type.Length > 50 ? item.Type.Substring(0, 47) + "..." : item.Type;
            var growthStr = item.Growth >= 0 
                ? $"+{_displayService.FormatBytes((ulong)item.Growth)}" 
                : $"-{_displayService.FormatBytes((ulong)Math.Abs(item.Growth))}";
            
            Console.WriteLine($"{shortName,-50} {growthStr,15}");
        }
        Console.WriteLine();
    }

    private string GetChangeIndicator(long oldValue, long newValue)
    {
        var change = newValue - oldValue;
        var percentChange = oldValue > 0 ? (change * 100.0 / oldValue) : 0;

        if (change > 0)
            return $"+{change:N0} (+{percentChange:N1}%)";
        else if (change < 0)
            return $"{change:N0} ({percentChange:N1}%)";
        else
            return "No change";
    }

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_detector is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}