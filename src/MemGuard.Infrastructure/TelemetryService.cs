using OpenTelemetry;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace MemGuard.Infrastructure;

/// <summary>
/// Provides telemetry for MemGuard operations
/// </summary>
public class TelemetryService : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _analysisCounter;
    private readonly Histogram<double> _analysisDuration;
    private readonly ActivitySource _activitySource;
    private bool _disposed;
    
    public TelemetryService()
    {
        _meter = new Meter("MemGuard.Infrastructure");
        _analysisCounter = _meter.CreateCounter<long>("analysis_count", "Number of analyses performed");
        _analysisDuration = _meter.CreateHistogram<double>("analysis_duration", "seconds", "Time taken for analysis");
        
        _activitySource = new ActivitySource("MemGuard.Infrastructure");
    }
    
    /// <summary>
    /// Records metrics for an analysis operation
    /// </summary>
    /// <param name="durationSeconds">Duration of the analysis in seconds</param>
    /// <param name="success">Whether the analysis was successful</param>
    public void RecordAnalysis(double durationSeconds, bool success)
    {
        _analysisCounter.Add(1, new KeyValuePair<string, object?>("success", success));
        _analysisDuration.Record(durationSeconds, new KeyValuePair<string, object?>("success", success));
    }
    
    /// <summary>
    /// Creates an activity for tracing an analysis operation
    /// </summary>
    /// <param name="dumpPath">Path to the dump being analyzed</param>
    /// <returns>Activity for the analysis</returns>
    public Activity? StartAnalysisActivity(string dumpPath)
    {
        var activity = _activitySource.StartActivity("AnalyzeDump");
        activity?.SetTag("dump.path", dumpPath);
        return activity;
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _meter?.Dispose();
                _activitySource?.Dispose();
            }
            _disposed = true;
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}