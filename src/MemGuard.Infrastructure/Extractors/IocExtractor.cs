using MemGuard.Core;
using MemGuard.Core.Interfaces;
using Microsoft.Diagnostics.Runtime;
using System.Text.RegularExpressions;

namespace MemGuard.Infrastructure.Extractors;

/// <summary>
/// Extractor that detects Indicators of Compromise (IOC) in memory dumps
/// </summary>
public class IocExtractor : IDiagnosticExtractor
{
    public string Name => "IOC";

    // Known malicious patterns (simplified for demonstration)
    private static readonly Dictionary<string, string> SuspiciousIpPatterns = new()
    {
        //Example known malicious IPs - in production, this would come from threat intel feeds
        { "192.0.2.", "Test Network" },
        { "198.51.100.", "Documentation Network" },
    };

    private static readonly string[] SuspiciousMutexNames = new[]
    {
        "Global\\\\MsMpEng",  // Common malware mutex
        "Global\\\\AVIRA",
        "_SOSI_MUTEX_",
        "DC_MUTEX",
    };

    private static readonly string[] SuspiciousRegistryPaths = new[]
    {
        "SOFTWARE\\\\Microsoft\\\\Windows\\\\CurrentVersion\\\\Run",
        "SOFTWARE\\\\Microsoft\\\\Windows\\\\CurrentVersion\\\\RunOnce",
        "Software\\\\Classes\\\\exefile\\\\shell\\\\open\\\\command",
    };

    private static readonly Regex IpAddressRegex = new(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled);
    private static readonly Regex DomainRegex = new(@"\b(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z]{2,}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<DiagnosticBase?> ExtractAsync(ClrRuntime runtime, CancellationToken cancellationToken = default)
    {
        var indicators = new List<IocMatch>();
        var stringData = await ExtractStringsFromMemoryAsync(runtime, cancellationToken);

        // Check for suspicious IP addresses
        var ipMatches = IpAddressRegex.Matches(stringData);
        foreach (Match match in ipMatches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ip = match.Value;
            
            foreach (var pattern in SuspiciousIpPatterns)
            {
                if (ip.StartsWith(pattern.Key))
                {
                    indicators.Add(new IocMatch(
                        "IP",
                        ip,
                        "Medium",
                        $"Suspicious IP address: {pattern.Value}"));
                }
            }
        }

        // Check for suspicious domains
        var domainMatches = DomainRegex.Matches(stringData);
        var suspiciousDomains = domainMatches.Cast<Match>()
            .Select(m => m.Value)
            .Where(d => d.Contains("temp") || d.Contains("malware") || d.Contains("c2"))
            .Distinct()
            .Take(10);

        foreach (var domain in suspiciousDomains)
        {
            indicators.Add(new IocMatch(
                "Domain",
                domain,
                "High",
                "Potentially malicious domain detected"));
        }

        // Check for suspicious mutex names
        foreach (var mutex in SuspiciousMutexNames)
        {
            if (stringData.Contains(mutex, StringComparison.OrdinalIgnoreCase))
            {
                indicators.Add(new IocMatch(
                    "Mutex",
                    mutex,
                    "Critical",
                    "Known malware mutex detected"));
            }
        }

        // Check for suspicious registry paths
        foreach (var regPath in SuspiciousRegistryPaths)
        {
            if (stringData.Contains(regPath, StringComparison.OrdinalIgnoreCase))
            {
                indicators.Add(new IocMatch(
                    "Registry",
                    regPath,
                    "High",
                    "Suspicious persistence mechanism registry key"));
            }
        }

        // Calculate threat score (0-100)
        var threatScore = CalculateThreatScore(indicators);

        return new IocDiagnostic(indicators, threatScore);
    }

    private async Task<string> ExtractStringsFromMemoryAsync(ClrRuntime runtime, CancellationToken cancellationToken)
    {
        var strings = new List<string>();
        var maxStrings = 10000; // Limit to avoid memory issues
        var count = 0;

        await Task.Run(() =>
        {
            try
            {
                foreach (var obj in runtime.Heap.EnumerateObjects())
                {
                    if (count++ > maxStrings)
                        break;

                    cancellationToken.ThrowIfCancellationRequested();

                    if (obj.Type?.Name == "System.String")
                    {
                        try
                        {
                            var str = obj.AsString();
                            if (!string.IsNullOrEmpty(str) && str.Length < 500)
                            {
                                strings.Add(str);
                            }
                        }
                        catch
                        {
                            // Ignore individual string read errors
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Continue with extracted strings
            }
        }, cancellationToken);

        return string.Join(" ", strings);
    }

    private int CalculateThreatScore(List<IocMatch> indicators)
    {
        if (indicators.Count == 0)
            return 0;

        var score = 0;
        var weights = new Dictionary<string, int>
        {
            { "Critical", 40 },
            { "High", 25 },
            { "Medium", 15 },
            { "Low", 5 }
        };

        foreach (var indicator in indicators)
        {
            if (weights.TryGetValue(indicator.ThreatLevel, out var weight))
            {
                score += weight;
            }
        }

        return Math.Min(score, 100);
    }
}
