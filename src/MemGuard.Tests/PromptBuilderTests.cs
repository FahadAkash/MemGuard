using MemGuard.Core;
using MemGuard.Core.Services;
using Xunit;

namespace MemGuard.Tests;

public class PromptBuilderTests
{
    [Fact]
    public void BuildAnalysisPrompt_ContainsDiagnosticInfo()
    {
        var diagnostics = new List<DiagnosticBase>
        {
            new HeapDiagnostic(0.5, 1000, 2000)
        };

        var prompt = PromptBuilder.BuildAnalysisPrompt(diagnostics);

        Assert.Contains("Heap", prompt);
        Assert.Contains("Fragmentation: 50.00%", prompt);
        Assert.Contains("Total Size: 2,000 bytes", prompt);
    }

    [Fact]
    public void ParseResponse_ValidJson_ReturnsAnalysisResult()
    {
        var json = """
            {
                "rootCause": "Memory leak in Cache",
                "codeFix": "Clear cache",
                "confidenceScore": 0.95
            }
            """;
        
        var result = PromptBuilder.ParseResponse(json, new List<DiagnosticBase>());

        Assert.Equal("Memory leak in Cache", result.RootCause);
        Assert.Equal("Clear cache", result.CodeFix);
        Assert.Equal(0.95, result.ConfidenceScore);
    }

    [Fact]
    public void ParseResponse_MarkdownJson_ReturnsAnalysisResult()
    {
        var json = """
            ```json
            {
                "rootCause": "Memory leak in Cache",
                "codeFix": "Clear cache",
                "confidenceScore": 0.95
            }
            ```
            """;
        
        var result = PromptBuilder.ParseResponse(json, new List<DiagnosticBase>());

        Assert.Equal("Memory leak in Cache", result.RootCause);
    }
}
