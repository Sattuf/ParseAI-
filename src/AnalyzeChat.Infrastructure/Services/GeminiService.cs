using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AnalyzeChat.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AnalyzeChat.Infrastructure.Services;

/// <summary>
/// Communicates with Google Gemini API for streaming text generation.
/// Uses the Gemini REST API with SSE streaming.
/// </summary>
public class GeminiService : IChatAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<GeminiService> _logger;
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
    private const int MaxRetries = 5;

    public GeminiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is not configured in appsettings.json");
        _model = configuration["Gemini:Model"] ?? "gemini-2.0-flash";
        _logger = logger;
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        string prompt,
        string model,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
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
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 4096,
            }
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var url = $"{BaseUrl}/{model}:streamGenerateContent?alt=sse&key={_apiKey}";

        // Retry loop for rate limiting
        HttpResponseMessage? response = null;
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (attempt > 0)
            {
                // Exponential backoff: 2s, 4s, 8s, 16s, 32s -> total ~62s to bypass 15 RPM limits
                var delay = (int)Math.Pow(2, attempt) * 1000; 
                _logger.LogWarning("Rate limited (429). Retrying in {Delay}ms (attempt {Attempt}/{Max})...",
                    delay, attempt, MaxRetries);
                await Task.Delay(delay, cancellationToken);
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode != HttpStatusCode.TooManyRequests)
                break;

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("429 response body: {Body}", errorBody);

            if (attempt < MaxRetries)
                response.Dispose();
        }

        if (response == null)
            throw new Exception("Failed to get response from Gemini API");

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Rate limit exceeded. Response: {Body}", body);
            response.Dispose();
            yield return $"⚠️ Gemini Rate Limit: {body}";
            yield break;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini API error {StatusCode}: {Body}", response.StatusCode, errorBody);
            response.Dispose();
            yield return $"⚠️ API Hatası ({(int)response.StatusCode}): {errorBody}";
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string jsonBuffer = "";

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith("data: "))
            {
                var jsonData = line["data: ".Length..];
                if (jsonData == "[DONE]") yield break;
                jsonBuffer += jsonData;
            }
            else
            {
                jsonBuffer += line;
            }

            if (string.IsNullOrWhiteSpace(jsonBuffer)) continue;

            string? textToYield = null;
            string? errorToYield = null;

            try
            {
                using var doc = JsonDocument.Parse(jsonBuffer);
                jsonBuffer = ""; // successfully parsed, clear buffer
                
                var root = doc.RootElement;

                // Gemini response format: candidates[0].content.parts[0].text
                if (root.TryGetProperty("candidates", out var candidates)
                    && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];

                    // Check finishReason
                    if (firstCandidate.TryGetProperty("finishReason", out var finishReasonElement))
                    {
                        var finishReason = finishReasonElement.GetString();
                        if (finishReason == "SAFETY")
                        {
                            errorToYield = "\n\n⚠️ **Content blocked due to safety settings.**";
                            _logger.LogWarning("Gemini generated content blocked. FinishReason: SAFETY");
                        }
                        else if (finishReason == "RECITATION")
                        {
                            errorToYield = "\n\n⚠️ **Content blocked due to recitation (copyright).**";
                            _logger.LogWarning("Gemini generated content blocked. FinishReason: RECITATION");
                        }
                        else if (finishReason != "STOP" && finishReason != null)
                        {
                            _logger.LogWarning("Gemini finishReason: {Reason}", finishReason);
                        }
                    }

                    if (errorToYield == null && firstCandidate.TryGetProperty("content", out var content)
                        && content.TryGetProperty("parts", out var parts)
                        && parts.GetArrayLength() > 0)
                    {
                        if (parts[0].TryGetProperty("text", out var textElement))
                        {
                            textToYield = textElement.GetString();
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Partial JSON, wait for more data lines
                continue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse SSE chunk from buffer: {Data}", jsonBuffer);
                jsonBuffer = ""; // clear buffer on unexpected error
                continue;
            }

            if (errorToYield != null)
            {
                yield return errorToYield;
                yield break;
            }

            if (!string.IsNullOrEmpty(textToYield))
            {
                yield return textToYield;
            }
        }

        response.Dispose();
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            content = new
            {
                parts = new[]
                {
                    new { text = text }
                }
            }
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        // Use text-embedding-004 for embeddings
        var url = $"{BaseUrl}/text-embedding-004:embedContent?key={_apiKey}";

        HttpResponseMessage? response = null;
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
             if (attempt > 0)
            {
                var delay = (int)Math.Pow(2, attempt) * 1000;
                _logger.LogWarning("Embedding Rate limited (429). Retrying in {Delay}ms (attempt {Attempt}/{Max})...",
                    delay, attempt, MaxRetries);
                await Task.Delay(delay, cancellationToken);
            }

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.StatusCode != HttpStatusCode.TooManyRequests)
                break;
            
            if (attempt < MaxRetries)
                response.Dispose();
        }

        if (response == null || !response.IsSuccessStatusCode)
        {
            var errorBody = await response!.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini Embedding API error {StatusCode}: {Body}", response.StatusCode, errorBody);
            throw new Exception($"Gemini Embedding API error: {response.StatusCode} - {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        
        // Response format: { "embedding": { "values": [0.1, 0.2, ...] } }
        if (doc.RootElement.TryGetProperty("embedding", out var embeddingElement) &&
            embeddingElement.TryGetProperty("values", out var valuesElement))
        {
            return valuesElement.EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();
        }

        throw new Exception("Invalid response format from Gemini Embedding API");
    }
}
