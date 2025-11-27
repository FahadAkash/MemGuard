using MemGuard.Core;
using MemGuard.Infrastructure;
using MemGuard.AI;
using MemGuard.AI.Interface;
using Microsoft.Diagnostics.Runtime;
using System.Diagnostics;
using MemGuard.Core.Interfaces;

namespace MemGuard.Cli;

/// <summary>
/// Orchestrates the entire analysis process
/// </summary>
public class AnalysisOrchestrator
{
    private readonly IEnumerable<IAnalyzer> _analyzers;
    private readonly ILLMClient _llmClient;
    private readonly TelemetryService _telemetryService;
    private readonly IMemLeakDetector _memLeakDetectorService;
    
    public AnalysisOrchestrator(
        IEnumerable<IAnalyzer> analyzers,
        ILLMClient llmClient,
        TelemetryService telemetryService , 
        IMemLeakDetector memLeakDetectorService)
    {
        _analyzers = analyzers;
        _llmClient = llmClient;
        _telemetryService = telemetryService;
        _memLeakDetectorService = memLeakDetectorService;
    }

    /// <summary>
    /// Analyzes a memory dump file
    /// </summary>
    /// <param name="dumpPath">Path to the dump file</param>
    /// <returns>Analysis result</returns>
    public async Task<AnalysisResult> AnalyzeDump(string dumpPath)
    {
        ArgumentNullException.ThrowIfNull(dumpPath);

        using var activity = _telemetryService.StartAnalysisActivity(dumpPath);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Load the dump
           _memLeakDetectorService.LoadDumpFile(dumpPath);

            // Extract diagnostics using analyzers
            var diagnostics = new List<DiagnosticBase>();
            
            // Use built-in extractors
            // var heapDiagnostic = DumpParser.ExtractHeapInfo();
            // diagnostics.Add(heapDiagnostic);
            
            // var deadlockDiagnostic = DumpParser.DetectDeadlocks(runtime);
            // if (deadlockDiagnostic.ThreadIds.Count > 0)
            //     diagnostics.Add(deadlockDiagnostic);

            // // Use strategy pattern analyzers
            // var context = new AnalysisContext { DumpPath = dumpPath };
            // foreach (var analyzer in _analyzers)
            // {
            //     var analyzerDiagnostics = analyzer.Analyze(context);
            //     diagnostics.AddRange(analyzerDiagnostics);
            // }

            // Build prompt for LLM
            var prompt = PromptBuilder.BuildAnalysisPrompt(diagnostics);

            // Get AI analysis
            var llmResponse = await _llmClient.GenerateResponseAsync(prompt).ConfigureAwait(false);

            // Parse response
            var result = PromptBuilder.ParseResponse(llmResponse, diagnostics);

            stopwatch.Stop();
            _telemetryService.RecordAnalysis(stopwatch.Elapsed.TotalSeconds, true);

            return result;
        }
        catch (ClrDiagnosticsException ex)
        {
            stopwatch.Stop();
            _telemetryService.RecordAnalysis(stopwatch.Elapsed.TotalSeconds, false);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            // Return a fallback result in case of error
            return new AnalysisResult(
                RootCause: $"Analysis failed: {ex.Message}",
                CodeFix: "Check the dump file and try again",
                ConfidenceScore: 0.0,
                Diagnostics: new List<DiagnosticBase>());
        }
        catch (FileNotFoundException ex)
        {
            stopwatch.Stop();
            _telemetryService.RecordAnalysis(stopwatch.Elapsed.TotalSeconds, false);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            // Return a fallback result in case of error
            return new AnalysisResult(
                RootCause: $"Dump file not found: {ex.Message}",
                CodeFix: "Verify the dump file path is correct",
                ConfidenceScore: 0.0,
                Diagnostics: new List<DiagnosticBase>());
        }
        catch (UnauthorizedAccessException ex)
        {
            stopwatch.Stop();
            _telemetryService.RecordAnalysis(stopwatch.Elapsed.TotalSeconds, false);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            // Return a fallback result in case of error
            return new AnalysisResult(
                RootCause: $"Access denied to dump file: {ex.Message}",
                CodeFix: "Check file permissions",
                ConfidenceScore: 0.0,
                Diagnostics: new List<DiagnosticBase>());
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _telemetryService.RecordAnalysis(stopwatch.Elapsed.TotalSeconds, false);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            // Return a fallback result in case of error
            return new AnalysisResult(
                RootCause: $"Analysis failed: {ex.Message}",
                CodeFix: "Check the dump file and try again",
                ConfidenceScore: 0.0,
                Diagnostics: new List<DiagnosticBase>());
        }
    }
}