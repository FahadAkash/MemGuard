using MemGuard.Core.Interfaces;
using Polly;
using Polly.Retry;
using System.Net.Http.Json;

namespace MemGuard.AI;

/// <summary>
/// DeepSeek implementation of ILLMClient
/// Uses DeepSeek for code-focused analysis and cost-effective inference
/// </summary>
public class DeepSeekClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly AsyncRetryPolicy _retryPolicy;

    public DeepSeekClient(HttpClient httpClient, string apiKey, string? model = null)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _model = model ?? "deepseek-chat";
        
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var url = "https://api.deepseek.com/v1/chat/completions";
        
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
                    throw new HttpRequestException($"DeepSeek API error ({response.StatusCode}): {errorContent}");
                }
                
                var json = await response.Content.ReadFromJsonAsync<DeepSeekResponse>(cancellationToken: cancellationToken);
                return json?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response from DeepSeek";
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"DeepSeek API call failed: {ex.Message}", ex);
            }
        });
    }

    private class DeepSeekResponse
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
