namespace AlignRoute.Api.Models;

public sealed class Attraction
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? ImageUrl { get; init; }
    public string OsmId { get; init; } = string.Empty;
    public int SortOrder { get; init; }

    public Session? Session { get; init; }
    public List<Vote> Votes { get; init; } = [];
}
