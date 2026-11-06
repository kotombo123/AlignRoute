namespace AlignRoute.Api.Models;

public sealed class Participant
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public string Nickname { get; init; } = string.Empty;
    public bool IsOrganizer { get; init; }
    public string ExcludedCategoriesJson { get; init; } = "[]";
    public bool HasFinishedVoting { get; init; }
    public DateTime JoinedAt { get; init; }

    public Session? Session { get; init; }
    public List<Vote> Votes { get; init; } = [];
}
