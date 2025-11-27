using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MemGuard.Core.Interfaces;
using Polly;
using Polly.Retry;

namespace MemGuard.AI;

/// <summary>
/// Gemini Flash implementation of ILLMClient
/// </summary>
public class GeminiClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly AsyncRetryPolicy _retryPolicy;

    public GeminiClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // Using v1beta with gemini-2.0-flash (matches working curl)
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
        
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                // Add API key as header (X-goog-api-key) instead of query parameter
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("X-goog-api-key", _apiKey);
                request.Content = JsonContent.Create(requestBody);
                
                var response = await _httpClient.SendAsync(request, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Gemini API error ({response.StatusCode}): {errorContent}");
                }
                
                var json = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);
                return json?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "No response from Gemini";
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Gemini API call failed: {ex.Message}", ex);
            }
        });
    }

    // Gemini API Response Models
    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
