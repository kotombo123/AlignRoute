namespace AlignRoute.Api.Services;

public sealed record OverpassPoi(
    string OsmId,
    string Name,
    double Lat,
    double Lon,
    string Category,
    IReadOnlyDictionary<string, string> Tags
);

public interface IOverpassService
{
    Task<List<OverpassPoi>> FindAttractionsAsync(string destination, double lat, double lon, CancellationToken ct = default);
}
