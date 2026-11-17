using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AlignRoute.Api.Models;

namespace AlignRoute.Api.Services;

public class ExplorerAgent
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ExplorerAgent> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ExplorerAgent(HttpClient httpClient, IConfiguration config, ILogger<ExplorerAgent> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<List<Attraction>> GenerateAttractionsAsync(int sessionId, string destination, string language)
    {
        var baseUrl = _config["LlmSettings:BaseUrl"]?.TrimEnd('/')
            ?? "https://llm.hpc.psnc.pl/v1";
        var apiKey = _config["LlmSettings:ApiKey"]
            ?? Environment.GetEnvironmentVariable("LLM_API_KEY")
            ?? throw new InvalidOperationException("Brak klucza API PSNC (LlmSettings:ApiKey lub LLM_API_KEY).");
        var model = _config["LlmSettings:Model"] ?? "gpt-oss_120b";
        var minCount = _config.GetValue<int>("LlmSettings:MinAttractions", 5);
        var maxCount = _config.GetValue<int>("LlmSettings:MaxAttractions", 15);
        var targetCount = (minCount + maxCount) / 2;

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var messages = new JsonArray
        {
            new JsonObject
            {
                ["role"] = "system",
                ["content"] = $$"""
                    Jesteś ekspertem turystycznym (AI-Eksplorator) w aplikacji AlignRoute.
                    Wygeneruj listę unikalnych atrakcji turystycznych dla podanej destynacji w języku: {{language}}.
                    Zwróć między {{minCount}} a {{maxCount}} propozycji.
                    Zwróć WYŁĄCZNIE czystą tablicę JSON (bez markdowna ```json ani żadnego tekstu przed/po) w formacie:
                    [
                      { "name": "Nazwa miejsca", "description": "Krótki opis (2 zdania)", "category": "Kategoria (np. Zabytki, Jedzenie, Natura, Kultura)" }
                    ]
                    """
            },
            new JsonObject
            {
                ["role"] = "user",
                ["content"] = $"Wygeneruj listę atrakcji dla destynacji: {destination}"
            }
        };

        // 1. Pierwsze wywołanie modelu PSNC
        var firstReply = await SendChatCompletionAsync($"{baseUrl}/chat/completions", model, messages);
        var attractions = ParseAttractionsFromContent(firstReply, sessionId);

        // 2. Walidacja liczby wyników -> jeśli poza zakresem [minCount, maxCount], wykonujemy korektę (drugi strzał)
        if (attractions.Count < minCount || attractions.Count > maxCount)
        {
            var reason = attractions.Count < minCount ? "za mało" : "za dużo";
            _logger.LogInformation("Korekta listy atrakcji ({Reason}): otrzymano {Count}, wymagane {Min}-{Max}.",
                reason, attractions.Count, minCount, maxCount);

            messages.Add(new JsonObject
            {
                ["role"] = "assistant",
                ["content"] = firstReply
            });

            messages.Add(new JsonObject
            {
                ["role"] = "user",
                ["content"] = $"[TOOL_RESULT: adjust_attractions_list] Zwrócono {attractions.Count} pozycji ({reason}). " +
                              $"Skoryguj poprzednią listę tak, aby zawierała dokładnie {targetCount} unikalnych atrakcji dla {destination} w języku {language}. " +
                              $"Zwróć WYŁĄCZNIE czystą tablicę JSON."
            });

            var secondReply = await SendChatCompletionAsync($"{baseUrl}/chat/completions", model, messages);
            attractions = ParseAttractionsFromContent(secondReply, sessionId);
        }

        return attractions;
    }

    private async Task<string> SendChatCompletionAsync(string endpointUrl, string model, JsonArray messages)
    {
        var payload = new JsonObject
        {
            ["model"] = model,
            ["messages"] = messages.DeepClone(),
            ["temperature"] = 0.7
        };

        using var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(endpointUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Błąd API PSNC ({StatusCode}): {Body}", response.StatusCode, responseBody);
            throw new HttpRequestException($"API PSNC zwróciło błąd {(int)response.StatusCode}: {responseBody}");
        }

        var root = JsonNode.Parse(responseBody)
            ?? throw new InvalidOperationException("Pusta odpowiedź z API PSNC.");

        return root["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? "[]";
    }

    private static List<Attraction> ParseAttractionsFromContent(string rawContent, int sessionId)
    {
        var cleaned = rawContent.Trim();
        var startIdx = cleaned.IndexOf('[');
        var endIdx = cleaned.LastIndexOf(']');
        if (startIdx >= 0 && endIdx > startIdx)
        {
            cleaned = cleaned[startIdx..(endIdx + 1)];
        }

        var dtos = JsonSerializer.Deserialize<List<AttractionDto>>(cleaned, JsonOptions) ?? new();

        return dtos.Select(d => new Attraction
        {
            SessionId = sessionId,
            Name = d.Name ?? "Nieznana atrakcja",
            Description = d.Description ?? string.Empty,
            Category = d.Category ?? "Ogólne"
        }).ToList();
    }

    private sealed class AttractionDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
    }
}