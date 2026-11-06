using AlignRoute.Api.Models;

namespace AlignRoute.Api.Services;

public interface IPlannerAgent
{
    Task<string> GeneratePlanAsync(Session session, List<Attraction> winningAttractions, CancellationToken ct = default);
}
