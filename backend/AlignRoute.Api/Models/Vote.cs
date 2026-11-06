namespace AlignRoute.Api.Models;

public sealed class Vote
{
    public Guid Id { get; init; }
    public Guid AttractionId { get; init; }
    public Guid ParticipantId { get; init; }
    public bool IsPositive { get; init; }
    public DateTime VotedAt { get; init; }

    public Attraction? Attraction { get; init; }
    public Participant? Participant { get; init; }
}
