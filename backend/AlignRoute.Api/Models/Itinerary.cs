namespace AlignRoute.Api.Models;

public sealed class Itinerary
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public DateTime GeneratedAt { get; init; }
    public string PlanJson { get; init; } = "[]";

    public Session? Session { get; init; }
}
