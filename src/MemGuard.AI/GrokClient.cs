using MemGuard.Core.Interfaces;
using Polly;
using Polly.Retry;
using System.Net.Http.Json;

namespace MemGuard.AI;

/// <summary>
/// Grok (xAI) implementation of ILLMClient
/// Uses Grok for real-time knowledge and code generation
/// </summary>
public class GrokClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly AsyncRetryPolicy _retryPolicy;

    public GrokClient(HttpClient httpClient, string apiKey, string? model = null)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _model = model ?? "grok-beta";
        
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var url = "https://api.x.ai/v1/chat/completions";
        
        var requestBody = new
        {
            model = _model,
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
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                request.Content = JsonContent.Create(requestBody);
                
                var response = await _httpClient.SendAsync(request, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Grok API error ({response.StatusCode}): {errorContent}");
                }
                
                var json = await response.Content.ReadFromJsonAsync<GrokResponse>(cancellationToken: cancellationToken);
                return json?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response from Grok";
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Grok API call failed: {ex.Message}", ex);
            }
        });
    }

    private class GrokResponse
    {
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        public Message? Message { get; set; }
    }

    private class Message
    {
        public string? Content { get; set; }
    }
}
