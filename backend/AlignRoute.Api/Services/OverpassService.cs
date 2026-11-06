using System.Globalization;
using System.Text.Json;

namespace AlignRoute.Api.Services;

public sealed class OverpassService : IOverpassService
{
    private const string OverpassUrl = "https://overpass-api.de/api/interpreter";
    private readonly HttpClient _httpClient;
    private readonly ILogger<OverpassService> _logger;

    public OverpassService(HttpClient httpClient, ILogger<OverpassService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<OverpassPoi>> FindAttractionsAsync(
        string destination, double lat, double lon, CancellationToken ct = default)
    {
        var query = BuildQuery(destination);
        _logger.LogInformation("Querying Overpass API for POIs near {Destination}", destination);

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("data", query)
        });

        HttpResponseMessage response;
        string responseBody;
        try
        {
            response = await _httpClient.PostAsync(OverpassUrl, formContent, ct);
            responseBody = await response.Content.ReadAsStringAsync(ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Overpass API request timed out for {Destination}", destination);
            return [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Network error contacting Overpass API for {Destination}", destination);
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Overpass API error: {StatusCode} - {Body}", response.StatusCode, responseBody);
            return [];
        }

        try
        {
            return ParseResponse(responseBody);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Overpass response for {Destination}", destination);
            return [];
        }
    }

    private static string BuildQuery(string destination)
    {
        var escapedDest = destination.Replace("\"", "\\\"");
        return $"""
            [out:json][timeout:30];
            area[name="{escapedDest}"]->.a;
            (
              node(area.a)["tourism"];
              node(area.a)["leisure"];
              node(area.a)["amenity"~"restaurant|cafe|bar|theatre|cinema"];
            );
            out body 50;
            """;
    }

    private List<OverpassPoi> ParseResponse(string json)
    {
        var results = new List<OverpassPoi>();

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("elements", out var elements))
        {
            _logger.LogWarning("Overpass response had no 'elements' property");
            return results;
        }

        foreach (var element in elements.EnumerateArray())
        {
            var tags = ParseTags(element);
            if (!tags.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name))
                continue;

            var osmId = element.TryGetProperty("id", out var idProp)
                ? idProp.ToString()
                : Guid.NewGuid().ToString();

            var lat = element.TryGetProperty("lat", out var latProp)
                ? latProp.GetDouble()
                : 0;

            var lon = element.TryGetProperty("lon", out var lonProp)
                ? lonProp.GetDouble()
                : 0;

            var category = InferCategory(tags);

            results.Add(new OverpassPoi(osmId, name, lat, lon, category, tags));
        }

        _logger.LogInformation("Parsed {Count} named POIs from Overpass", results.Count);
        return results;
    }

    private static Dictionary<string, string> ParseTags(JsonElement element)
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!element.TryGetProperty("tags", out var tagsElement))
            return tags;

        foreach (var prop in tagsElement.EnumerateObject())
        {
            tags[prop.Name] = prop.Value.GetString() ?? string.Empty;
        }

        return tags;
    }

    private static string InferCategory(IReadOnlyDictionary<string, string> tags)
    {
        if (tags.TryGetValue("tourism", out var tourism))
        {
            return tourism switch
            {
                "museum" => "museum",
                "attraction" => "monument",
                "viewpoint" => "nature",
                "artwork" => "culture",
                "gallery" => "culture",
                "hotel" or "hostel" or "guest_house" => "other",
                _ => "other"
            };
        }

        if (tags.TryGetValue("leisure", out var leisure))
        {
            return leisure switch
            {
                "park" or "garden" => "park",
                "sports_centre" or "stadium" or "pitch" => "sport",
                "nature_reserve" => "nature",
                _ => "other"
            };
        }

        if (tags.TryGetValue("amenity", out var amenity))
        {
            return amenity switch
            {
                "restaurant" or "cafe" => "restaurant",
                "bar" or "pub" or "nightclub" => "nightlife",
                "theatre" or "cinema" => "culture",
                _ => "other"
            };
        }

        return "other";
    }
}
