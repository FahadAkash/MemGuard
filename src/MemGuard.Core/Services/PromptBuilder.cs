using System.Text;
using System.Text.Json;
using MemGuard.Core;

namespace MemGuard.Core.Services;

/// <summary>
/// Builds prompts for LLM analysis
/// </summary>
public static class PromptBuilder
{
    /// <summary>
    /// Builds a prompt for analyzing diagnostics
    /// </summary>
    /// <param name="diagnostics">Diagnostic information to analyze</param>
    /// <returns>Built prompt</returns>
    public static string BuildAnalysisPrompt(IReadOnlyList<DiagnosticBase> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var sb = new StringBuilder();
        sb.AppendLine("You are an expert .NET debugger. Analyze the following diagnostic information from a memory dump and provide:");
        sb.AppendLine("1. A plain-English root cause explanation");
        sb.AppendLine("2. A diff-style code fix with exact line numbers");
        sb.AppendLine("3. A confidence score (0.0 to 1.0)");
        sb.AppendLine();
        sb.AppendLine("Diagnostic Information:");

        foreach (var diagnostic in diagnostics)
        {
            sb.AppendLine($"- Type: {diagnostic.Type}");
            sb.AppendLine($"  Severity: {diagnostic.Severity}");
            sb.AppendLine($"  Description: {diagnostic.Description}");
            
            if (diagnostic is HeapDiagnostic heap)
            {
                sb.AppendLine($"  Fragmentation: {heap.FragmentationLevel:P2}");
                sb.AppendLine($"  Total Size: {heap.TotalSize:N0} bytes");
            }
            // Add other specific details if needed
        }

        sb.AppendLine();
        sb.AppendLine("Response in JSON format with keys: rootCause, codeFix, confidenceScore.");
        sb.AppendLine("Do not include markdown formatting like ```json ... ``` in the response, just raw JSON.");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Builds a prompt for requesting code fixes
    /// </summary>
    /// <param name="diagnostics">Diagnostic information</param>
    /// <param name="projectPath">Path to the project (optional)</param>
    /// <returns>Built prompt for fixes</returns>
    public static string BuildFixPrompt(IReadOnlyList<DiagnosticBase> diagnostics, string? projectPath = null)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var sb = new StringBuilder();
        sb.AppendLine("You are an expert .NET debugger and code fixer. Analyze the following diagnostic information and provide ACTIONABLE code fixes.");
        sb.AppendLine();
        sb.AppendLine("For each issue, provide:");
        sb.AppendLine("1. The EXACT file path (relative to project root)");
        sb.AppendLine("2. The complete fixed code in a code block");
        sb.AppendLine("3. A brief explanation of the fix");
        sb.AppendLine();
        sb.AppendLine("Format your response like this:");
        sb.AppendLine("File: path/to/File.cs");
        sb.AppendLine("```csharp");
        sb.AppendLine("// Fixed code here");
        sb.AppendLine("```");
        sb.AppendLine("Explanation: Brief explanation");
        sb.AppendLine();
        
        if (projectPath != null)
        {
            sb.AppendLine($"Project Path: {projectPath}");
            sb.AppendLine();
        }

        sb.AppendLine("Diagnostic Information:");
        foreach (var diagnostic in diagnostics)
        {
            sb.AppendLine($"- Type: {diagnostic.Type}");
            sb.AppendLine($"  Severity: {diagnostic.Severity}");
            sb.AppendLine($"  Description: {diagnostic.Description}");
            
            if (diagnostic is HeapDiagnostic heap)
            {
                sb.AppendLine($"  Fragmentation: {heap.FragmentationLevel:P2}");
                sb.AppendLine($"  Total Size: {heap.TotalSize:N0} bytes");
            }
            else if (diagnostic is DeadlockDiagnostic deadlock)
            {
                sb.AppendLine($"  Threads: {string.Join(", ", deadlock.ThreadIds)}");
                foreach (var lockInfo in deadlock.LockObjects.Take(5))
                {
                    sb.AppendLine($"  - {lockInfo}");
                }
            }
        }

        return sb.ToString();
    }
    
    /// <summary>
    /// Parses the LLM response into an AnalysisResult
    /// </summary>
    /// <param name="response">LLM response</param>
    /// <param name="diagnostics">Original diagnostics</param>
    /// <returns>AnalysisResult</returns>
    public static AnalysisResult ParseResponse(string response, IReadOnlyList<DiagnosticBase> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return new AnalysisResult(
                RootCause: "No response from AI model",
                CodeFix: "No code fix available",
                ConfidenceScore: 0.0,
                Diagnostics: diagnostics);
        }

        try
        {
            // Clean up markdown if present
            var json = response.Trim();
            if (json.StartsWith("```json")) json = json.Substring(7);
            if (json.StartsWith("```")) json = json.Substring(3);
            if (json.EndsWith("```")) json = json.Substring(0, json.Length - 3);
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<AIResponse>(json, options);
            
            return new AnalysisResult(
                RootCause: result?.RootCause ?? "Could not parse root cause",
                CodeFix: result?.CodeFix ?? "No fix provided",
                ConfidenceScore: result?.ConfidenceScore ?? 0.0,
                Diagnostics: diagnostics);
        }
        catch (Exception)
        {
            // Fallback
            return new AnalysisResult(
                RootCause: "Failed to parse AI response. Raw response: " + response,
                CodeFix: "N/A",
                ConfidenceScore: 0.0,
                Diagnostics: diagnostics);
        }
    }

    private class AIResponse
    {
        public string? RootCause { get; set; }
        public string? CodeFix { get; set; }
        public double ConfidenceScore { get; set; }
    }
}
