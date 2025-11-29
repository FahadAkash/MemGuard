using MemGuard.Core.Interfaces;

namespace MemGuard.AI;

/// <summary>
/// Factory for creating LLM client instances based on provider name
/// </summary>
public class LLMProviderFactory
{
    private readonly IHttpClientFactory _httpClientFactory;

    public LLMProviderFactory(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public ILLMClient CreateClient(string provider, string apiKey, string? model = null, string? baseUrl = null)
    {
        var httpClient = _httpClientFactory.CreateClient(provider);
        
        return provider.ToLowerInvariant() switch
        {
            "gemini" => new GeminiClient(httpClient, apiKey, model),
            "claude" => new ClaudeClient(httpClient, apiKey, model),
            "grok" => new GrokClient(httpClient, apiKey, model),
            "deepseek" => new DeepSeekClient(httpClient, apiKey, model),
            "ollama" => new OllamaClient(baseUrl ?? "http://localhost:11434", model ?? "llama3.2"),
            _ => throw new ArgumentException($"Unknown AI provider: {provider}. Supported providers: gemini, claude, grok, deepseek, ollama")
        };
    }

    public static string[] GetSupportedProviders()
    {
        return new[] { "gemini", "claude", "grok", "deepseek", "ollama" };
    }

    public static string GetDefaultModel(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "gemini" => "gemini-2.0-flash",
            "claude" => "claude-3-5-sonnet-20241022",
            "grok" => "grok-beta",
            "deepseek" => "deepseek-chat",
            "ollama" => "llama3.2",
            _ => "default"
        };
    }
}
