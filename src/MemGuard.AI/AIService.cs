using System.Text.Json;
using MemGuard.Core;
using Microsoft.Extensions.AI;
using System.Globalization;
using MemGuard.AI.Interface;

namespace MemGuard.AI;
/// <summary>
/// Ollama implementation of ILLMClient
/// </summary>
public class OllamaClient : ILLMClient
{
    private readonly string _model;
    
    public OllamaClient(Uri baseUri, string model)
    {
        _model = model;
    }
    
    public async Task<string> GenerateResponseAsync(string prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        // Simplified implementation for demonstration
        // In a real implementation, we would call the Ollama API
        await Task.Delay(100).ConfigureAwait(false); // Simulate network delay
        return $"Simulated AI response for model {_model} with prompt: {prompt.Substring(0, Math.Min(50, prompt.Length))}...";
    }
}

/// <summary>
/// Azure OpenAI implementation of ILLMClient
/// </summary>
public class AzureOpenAIClient : ILLMClient
{
    private readonly IChatClient _client;
    
    public AzureOpenAIClient(IChatClient client)
    {
        _client = client;
    }
    
    public async Task<string> GenerateResponseAsync(string prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        // Simplified implementation for demonstration
        await Task.Delay(100).ConfigureAwait(false); // Simulate network delay
        return $"Simulated Azure OpenAI response for prompt: {prompt.Substring(0, Math.Min(50, prompt.Length))}...";
    }
}

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

        var prompt = @"You are an expert .NET debugger. Analyze the following diagnostic information from a memory dump and provide:
1. A plain-English root cause explanation
2. A diff-style code fix with exact line numbers
3. A confidence score (0.0 to 1.0)

Diagnostic Information:
";
        
        foreach (var diagnostic in diagnostics)
        {
            prompt += $"{diagnostic.Type}: {diagnostic.Description} (Severity: {diagnostic.Severity})\n";
        }
        
        prompt += "\nResponse in JSON format with keys: rootCause, codeFix, confidenceScore";
        return prompt;
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
            // Simulate a successful JSON response
            return new AnalysisResult(
                RootCause: "Memory leak detected in UserManager class due to improper disposal of resources",
                CodeFix: "@@ -45,7 +45,7 @@\n public class UserManager {\n     private List<User> _users = new List<User>();\n     \n-    public void AddUser(User user) {\n+    public void AddUser(User user) {\n         _users.Add(user);\n         // TODO: Implement proper cleanup\n     }",
                ConfidenceScore: 0.85,
                Diagnostics: diagnostics);
        }
        catch (JsonException)
        {
            // Fallback if JSON parsing fails
            return new AnalysisResult(
                RootCause: response,
                CodeFix: "Refer to root cause for fix suggestions",
                ConfidenceScore: 0.5,
                Diagnostics: diagnostics);
        }
        catch (InvalidOperationException)
        {
            // Fallback for other parsing issues
            return new AnalysisResult(
                RootCause: response,
                CodeFix: "Refer to root cause for fix suggestions",
                ConfidenceScore: 0.5,
                Diagnostics: diagnostics);
        }
    }
}