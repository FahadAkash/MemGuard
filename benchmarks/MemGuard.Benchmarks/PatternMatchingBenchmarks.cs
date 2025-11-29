using BenchmarkDotNet.Attributes;
using System.Text.RegularExpressions;

namespace MemGuard.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class PatternMatchingBenchmarks
{
    private readonly Regex _ipRegex = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b", RegexOptions.Compiled);
    private readonly Regex _domainRegex = new Regex(@"[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62})+", RegexOptions.Compiled);
    private string _testData = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Generate test data with some malicious patterns
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 1000; i++)
        {
            sb.AppendLine($"Connection from 192.168.1.{i % 256}");
            sb.AppendLine($"Domain: test{i}.example.com");
            sb.AppendLine($"Mutex: Global\\MyMutex{i}");
            sb.AppendLine($"Registry: HKLM\\Software\\Test{i}");
        }
        _testData = sb.ToString();
    }

    [Benchmark(Description = "IP Address Pattern Matching (1000 entries)")]
    public int IpPatternMatching()
    {
        return _ipRegex.Matches(_testData).Count;
    }

    [Benchmark(Description = "Domain Pattern Matching (1000 entries)")]
    public int DomainPatternMatching()
    {
        return _domainRegex.Matches(_testData).Count;
    }

    [Benchmark(Description = "String Contains Check (1000 entries)")]
    public int StringContainsCheck()
    {
        int count = 0;
        var lines = _testData.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("Mutex") || line.Contains("Registry"))
            {
                count++;
            }
        }
        return count;
    }
}
