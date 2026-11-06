using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlignRoute.Api.Services;

public sealed class LlmSettings
{
    public string BaseUrl { get; init; } = "https://llm.hpc.psnc.pl";
    public string Model { get; init; } = "gpt-oss_120b";
    public string ApiKey { get; init; } = string.Empty;
}

public sealed class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly LlmSettings _settings;
    private readonly ILogger<LlmService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LlmService(HttpClient httpClient, LlmSettings settings, ILogger<LlmService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    public async Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException(
                "LLM API key is not configured. Set LlmSettings:ApiKey in appsettings or LLM_API_KEY env var.");
        }

        var requestBody = new
        {
            model = _settings.Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v1/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        _logger.LogInformation("Sending LLM request to {Url}, model={Model}", _settings.BaseUrl, _settings.Model);

        HttpResponseMessage response;
        string responseContent;
        try
        {
            response = await _httpClient.SendAsync(request, ct);
            responseContent = await response.Content.ReadAsStringAsync(ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "LLM request timed out after HttpClient timeout");
            throw new HttpRequestException(
                "The AI service did not respond in time. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error contacting LLM API at {Url}", _settings.BaseUrl);
            throw new HttpRequestException(
                "Could not reach the AI service. Please try again later.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("LLM API error: {StatusCode} - {Body}", response.StatusCode, responseContent);
            throw new HttpRequestException(
                $"The AI service returned an error (status {(int)response.StatusCode}). Please try again.");
        }

        try
        {
            using var doc = JsonDocument.Parse(responseContent);
            var choices = doc.RootElement.GetProperty("choices");

            if (choices.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("LLM returned no choices.");
            }

            var messageContent = choices[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return messageContent ?? string.Empty;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM response as JSON: {Body}", responseContent);
            throw new InvalidOperationException(
                "The AI service returned an unexpected response format. Please try again.");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "LLM response missing expected fields: {Body}", responseContent);
            throw new InvalidOperationException(
                "The AI service returned an incomplete response. Please try again.");
        }
    }
}
