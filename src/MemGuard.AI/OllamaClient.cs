using MemGuard.Core.Interfaces;
using Polly;
using Polly.Retry;

namespace MemGuard.AI;

/// <summary>
/// Ollama implementation of ILLMClient with Polly resilience
/// NOTE: Currently using mock implementation due to OllamaSharp 2.0 API instability.
/// For production, update when OllamaSharp API stabilizes.
/// </summary>
public class OllamaClient : ILLMClient
{
    private readonly string _baseUrl;
    private readonly string _model;
    private readonly AsyncRetryPolicy _retryPolicy;

    public OllamaClient(string baseUrl, string model)
    {
        _baseUrl = baseUrl;
        _model = model;
        
        // Retry 3 times with exponential backoff
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            // Mock implementation - returns helpful diagnostic message
            // In production, this would call actual Ollama API
            await Task.Delay(100, cancellationToken);
            
            return $@"[Ollama Mock Response - Model: {_model}]

**Root Cause Analysis:**
Based on the diagnostic data, the primary issue appears to be related to memory management patterns in the application.

**Suggested Actions:**
1. Review object lifecycle management
2. Check for event handler leaks
3. Analyze thread synchronization
4. Monitor GC pressure

**Note:** This is a mock response. For real analysis, ensure Ollama is running with model '{_model}' installed.
To use real Ollama: docker run -d -p 11434:11434 ollama/ollama && ollama pull {_model}";
        });
    }
}
