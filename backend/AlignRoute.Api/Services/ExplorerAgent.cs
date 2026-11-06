using System.Text.Json;
using AlignRoute.Api.Models;

namespace AlignRoute.Api.Services;

public sealed class ExplorerAgent : IExplorerAgent
{
    private readonly IOverpassService _overpassService;
    private readonly ILlmService _llmService;
    private readonly ILogger<ExplorerAgent> _logger;
    private readonly int _maxAttractions;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ExplorerAgent(
        IOverpassService overpassService,
        ILlmService llmService,
        IConfiguration configuration,
        ILogger<ExplorerAgent> logger)
    {
        _overpassService = overpassService;
        _llmService = llmService;
        _logger = logger;
        _maxAttractions = configuration.GetValue("SessionSettings:MaxAttractions", 20);
    }

    public async Task<List<Attraction>> ExploreAsync(Session session, CancellationToken ct = default)
    {
        var pois = await _overpassService.FindAttractionsAsync(
            session.Destination, session.DestinationLat, session.DestinationLon, ct);

        if (pois.Count == 0)
        {
            _logger.LogWarning("No POIs found for {Destination}, generating via LLM only", session.Destination);
            return await GenerateAttractionsFromLlmOnly(session, ct);
        }

        var poiSummary = string.Join("\n", pois.Select(p =>
            $"- {p.Name} (category: {p.Category}, lat: {p.Lat}, lon: {p.Lon})"));

        var systemPrompt = BuildSystemPrompt(session.Language);
        var userPrompt = BuildUserPrompt(session.Destination, poiSummary, session.Language);

        try
        {
            var llmResponse = await _llmService.ChatAsync(systemPrompt, userPrompt, ct);
            var enriched = ParseLlmAttractions(llmResponse, session.Id, pois);

            if (enriched.Count == 0)
            {
                _logger.LogWarning(
                    "LLM returned no valid attractions for {Destination}, falling back to LLM-only generation",
                    session.Destination);
                return await GenerateAttractionsFromLlmOnly(session, ct);
            }

            _logger.LogInformation("Explorer agent produced {Count} attractions for session {SessionId}",
                enriched.Count, session.Id);

            return enriched.Take(_maxAttractions).ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            _logger.LogWarning(ex,
                "LLM enrichment failed for {Destination}, falling back to LLM-only generation",
                session.Destination);
            return await GenerateAttractionsFromLlmOnly(session, ct);
        }
    }

    private async Task<List<Attraction>> GenerateAttractionsFromLlmOnly(Session session, CancellationToken ct)
    {
        var systemPrompt = BuildSystemPrompt(session.Language);
        var jsonExample = "[{\"name\":\"...\",\"description\":\"...\",\"category\":\"...\",\"lat\":0.0,\"lon\":0.0}]";
        var userPrompt = $"""
            There are no pre-loaded points of interest. Please suggest the top {_maxAttractions} tourist attractions
            in {session.Destination}. For each, provide: name, a compelling 2-sentence description
            (in {session.Language}), category (one of: museum, monument, park, restaurant, nightlife,
            shopping, nature, sport, culture, other), and approximate latitude/longitude.
            Return as a JSON array: {jsonExample}
            """;

        var llmResponse = await _llmService.ChatAsync(systemPrompt, userPrompt, ct);
        return ParseLlmGeneratedAttractions(llmResponse, session.Id);
    }

    private static string BuildSystemPrompt(string language) =>
        $"""
        You are a travel guide expert. You help tourists discover the best attractions.
        Always respond with valid JSON only — no markdown, no explanation outside the JSON.
        Use the language: {language}.
        """;

    private static string BuildUserPrompt(string destination, string poiSummary, string language)
    {
        var jsonExample = "[{\"name\":\"...\",\"description\":\"...\",\"category\":\"...\"}]";
        return $"""
            Given these points of interest near {destination}:

            {poiSummary}

            Select the top 20 most interesting ones for tourists. For each, provide:
            - name (original name)
            - description (a compelling 2-sentence description in {language})
            - category (one of: museum, monument, park, restaurant, nightlife, shopping, nature, sport, culture, other)

            Return as a JSON array: {jsonExample}
            """;
    }

    private List<Attraction> ParseLlmAttractions(string llmResponse, Guid sessionId, List<OverpassPoi> pois)
    {
        var jsonText = ExtractJsonArray(llmResponse);
        var items = DeserializeAttractionItems(jsonText);
        var poiLookup = pois.ToDictionary(p => p.Name.ToLowerInvariant(), p => p);

        return items
            .Select((item, index) =>
            {
                poiLookup.TryGetValue(item.Name?.ToLowerInvariant() ?? "", out var matchedPoi);
                return new Attraction
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    Name = item.Name ?? "Unknown",
                    Description = item.Description ?? "",
                    Category = NormalizeCategory(item.Category),
                    Latitude = matchedPoi?.Lat ?? item.Lat,
                    Longitude = matchedPoi?.Lon ?? item.Lon,
                    ImageUrl = null,
                    OsmId = matchedPoi?.OsmId ?? "",
                    SortOrder = index
                };
            })
            .ToList();
    }

    private List<Attraction> ParseLlmGeneratedAttractions(string llmResponse, Guid sessionId)
    {
        var jsonText = ExtractJsonArray(llmResponse);
        var items = DeserializeAttractionItems(jsonText);

        return items
            .Select((item, index) => new Attraction
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Name = item.Name ?? "Unknown",
                Description = item.Description ?? "",
                Category = NormalizeCategory(item.Category),
                Latitude = item.Lat,
                Longitude = item.Lon,
                ImageUrl = null,
                OsmId = "",
                SortOrder = index
            })
            .ToList();
    }

    private List<LlmAttractionItem> DeserializeAttractionItems(string jsonText)
    {
        try
        {
            return JsonSerializer.Deserialize<List<LlmAttractionItem>>(jsonText, JsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM attraction response: {Text}", jsonText);
            return [];
        }
    }

    private static string ExtractJsonArray(string text)
    {
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start < 0 || end < 0 || end <= start)
            return "[]";

        return text[start..(end + 1)];
    }

    private static string NormalizeCategory(string? category)
    {
        var valid = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "museum", "monument", "park", "restaurant", "nightlife",
            "shopping", "nature", "sport", "culture", "other"
        };

        return valid.Contains(category ?? "") ? category!.ToLowerInvariant() : "other";
    }

    private sealed record LlmAttractionItem(
        string? Name,
        string? Description,
        string? Category,
        double Lat = 0,
        double Lon = 0
    );
}
