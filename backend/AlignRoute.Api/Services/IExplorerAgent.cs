using AlignRoute.Api.Models;

namespace AlignRoute.Api.Services;

public interface IExplorerAgent
{
    Task<List<Attraction>> ExploreAsync(Session session, CancellationToken ct = default);
}
