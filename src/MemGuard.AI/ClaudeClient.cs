using MemGuard.Core.Interfaces;
using Polly;
using Polly.Retry;
using System.Net.Http.Json;

namespace MemGuard.AI;

/// <summary>
/// Claude (Anthropic) implementation of ILLMClient
/// Uses Claude 3.5 Sonnet for advanced reasoning and code analysis
/// </summary>
public class ClaudeClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly AsyncRetryPolicy _retryPolicy;

    public ClaudeClient(HttpClient httpClient, string apiKey, string? model = null)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _model = model ?? "claude-3-5-sonnet-20241022";
        
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var url = "https://api.anthropic.com/v1/messages";
        
        var requestBody = new
        {
            model = _model,
            max_tokens = 4096,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("x-api-key", _apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                request.Content = JsonContent.Create(requestBody);
                
                var response = await _httpClient.SendAsync(request, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Claude API error ({response.StatusCode}): {errorContent}");
                }
                
                var json = await response.Content.ReadFromJsonAsync<ClaudeResponse>(cancellationToken: cancellationToken);
                return json?.Content?.FirstOrDefault()?.Text ?? "No response from Claude";
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Claude API call failed: {ex.Message}", ex);
            }
        });
    }

    private class ClaudeResponse
    {
        public List<ContentBlock>? Content { get; set; }
    }

    private class ContentBlock
    {
        public string? Text { get; set; }
    }
}
