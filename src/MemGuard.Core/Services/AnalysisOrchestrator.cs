using MemGuard.Core.Interfaces;
using MemGuard.Core.Models;

namespace MemGuard.Core.Services;

public class AnalysisOrchestrator
{
    private readonly IDumpParser _dumpParser;
    private readonly IEnumerable<IDiagnosticExtractor> _extractors;
    private readonly ILLMClient _llmClient;

    public AnalysisOrchestrator(
        IDumpParser dumpParser,
        IEnumerable<IDiagnosticExtractor> extractors,
        ILLMClient llmClient)
    {
        _dumpParser = dumpParser;
        _extractors = extractors;
        _llmClient = llmClient;
    }

    public async Task<AnalysisResult> AnalyzeAsync(string dumpPath, CancellationToken cancellationToken = default)
    {
        // 1. Load Dump
        using var runtime = _dumpParser.LoadDump(dumpPath);
        
        // 2. Run Extractors
        var diagnostics = new List<DiagnosticBase>();
        foreach (var extractor in _extractors)
        {
            try
            {
                var diagnostic = await extractor.ExtractAsync(runtime, cancellationToken);
                if (diagnostic != null)
                {
                    diagnostics.Add(diagnostic);
                }
            }
            catch (Exception ex)
            {
                // Log error but continue
                Console.WriteLine($"Extractor {extractor.Name} failed: {ex.Message}");
            }
        }

        if (diagnostics.Count == 0)
        {
            return new AnalysisResult(
                RootCause: "No issues detected by extractors.",
                CodeFix: "",
                ConfidenceScore: 1.0,
                Diagnostics: diagnostics);
        }

        // 3. Build Prompt
        var prompt = PromptBuilder.BuildAnalysisPrompt(diagnostics);
        
        // 4. Get AI Analysis
        var aiResponse = await _llmClient.GenerateResponseAsync(prompt, cancellationToken);
        
        // 5. Parse Response
        return PromptBuilder.ParseResponse(aiResponse, diagnostics);
    }
}
