using System;
using MemGuard.Core.Services;

namespace MemGuard.Core;

public static class TestProgram
{

    public static void Main()
    {
        Console.WriteLine("Testing MemGuard Core Components");
        using var detector = new MemLeakDetectorService(dumpPath: @"F:\gihtub\MemGuard\DumpFile\MultiTenantBilling.Api.dmp");
        var options = new AnalysisOptions
        {
            TopN = 20,
            MaxSamplesPerType = 5
        };

        var result = detector.ExecuteAnalysis(options);
        Console.WriteLine($"Leak Report Generated at {DateTime.Now}");
        Console.WriteLine($"Root Cause: {result.RootCause}");
        Console.WriteLine($"Confidence: {result.ConfidenceScore:P0}");
        Console.WriteLine($"\nSuggested Fix:\n{result.CodeFix}");
         
      
        Console.WriteLine($"Analysis Result:");
        Console.WriteLine($"  Root Cause: {result.RootCause}");
        Console.WriteLine($"  Confidence: {result.ConfidenceScore:P2}");
        Console.WriteLine($"  Diagnostics: {result.Diagnostics.Count}");

        foreach (var diag in result.Diagnostics)
        {
            Console.WriteLine($"    - {diag.Type}: {diag.Description}");
        }
    }
}