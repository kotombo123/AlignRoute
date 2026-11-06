using System.Text.Json;
using AlignRoute.Api.Models;

namespace AlignRoute.Api.Services;

public sealed class PlannerAgent : IPlannerAgent
{
    private readonly ILlmService _llmService;
    private readonly ILogger<PlannerAgent> _logger;

    public PlannerAgent(ILlmService llmService, ILogger<PlannerAgent> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<string> GeneratePlanAsync(
        Session session, List<Attraction> winningAttractions, CancellationToken ct = default)
    {
        if (winningAttractions.Count == 0)
        {
            _logger.LogWarning("No winning attractions for session {SessionId}", session.Id);
            return "[]";
        }

        var systemPrompt = BuildSystemPrompt(session.Language);
        var userPrompt = BuildUserPrompt(session, winningAttractions);

        string llmResponse;
        try
        {
            llmResponse = await _llmService.ChatAsync(systemPrompt, userPrompt, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "LLM call failed during plan generation for session {SessionId}", session.Id);
            throw new InvalidOperationException(
                "Could not reach the AI service to generate the plan. Please try again.", ex);
        }

        var planJson = ExtractJsonArray(llmResponse);

        if (!IsValidJson(planJson))
        {
            _logger.LogError("LLM returned invalid plan JSON for session {SessionId}: {Response}",
                session.Id, llmResponse);
            throw new InvalidOperationException(
                "The AI planner returned an invalid response. Please try again.");
        }

        _logger.LogInformation("Planner generated plan for session {SessionId} with {Count} attractions",
            session.Id, winningAttractions.Count);

        return planJson;
    }

    private static string BuildSystemPrompt(string language) =>
        $"""
        You are a travel logistics expert. You optimize travel itineraries for efficiency and enjoyment.
        Always respond with valid JSON only — no markdown, no explanation outside the JSON.
        Use the language: {language} for any textual notes.
        """;

    private static string BuildUserPrompt(Session session, List<Attraction> attractions)
    {
        var attractionLines = string.Join("\n", attractions.Select(a =>
            $"- id: {a.Id}, name: {a.Name}, lat: {a.Latitude}, lon: {a.Longitude}, category: {a.Category}"));

        var jsonExample = """
            [{"day":1,"date":null,"attractions":[{"id":"<attraction-guid>","name":"...","suggestedTime":"10:00","durationMinutes":60,"notes":"..."}]}]
            """.Trim();
        return $"""
            Plan a {session.DurationDays}-day trip visiting these attractions in {session.Destination}.
            Consider distances between locations to minimize travel time. Group nearby attractions into the same day.
            For each day, list attractions in optimal visiting order with suggested times.

            Attractions:
            {attractionLines}

            Return as JSON array matching this schema: {jsonExample}
            """;
    }

    private static string ExtractJsonArray(string text)
    {
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start < 0 || end < 0 || end <= start)
            return "[]";

        return text[start..(end + 1)];
    }

    private static bool IsValidJson(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            return doc.RootElement.ValueKind == JsonValueKind.Array;
        }
        catch
        {
            return false;
        }
    }
}
