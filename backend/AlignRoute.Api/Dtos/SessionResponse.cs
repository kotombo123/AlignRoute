using AlignRoute.Api.Models;

namespace AlignRoute.Api.Dtos;

public sealed record SessionResponse(
    Guid Id,
    string JoinCode,
    string Destination,
    double DestinationLat,
    double DestinationLon,
    int DurationDays,
    string Status,
    string Language,
    DateTime CreatedAt,
    List<ParticipantResponse> Participants
);

public sealed record ParticipantResponse(
    Guid Id,
    string Nickname,
    bool IsOrganizer,
    bool HasFinishedVoting,
    DateTime JoinedAt
);

public sealed record CreateSessionResponse(
    Guid SessionId,
    string JoinCode,
    Guid ParticipantId,
    SessionResponse Session
);

public sealed record JoinSessionResponse(
    Guid ParticipantId,
    SessionResponse Session
);

public sealed record AttractionResponse(
    Guid Id,
    string Name,
    string Description,
    string Category,
    double Latitude,
    double Longitude,
    string? ImageUrl,
    int SortOrder,
    int YesVotes,
    int NoVotes
);

public sealed record VoteCardResponse(
    Guid Id,
    string Name,
    string Description,
    string Category,
    double Latitude,
    double Longitude,
    string? ImageUrl,
    int SortOrder
);

public sealed record StatusResponse(
    string Status,
    List<ParticipantResponse> Participants,
    int AttractionCount,
    int TotalVotesCast
);

public static class ResponseMapper
{
    public static SessionResponse ToSessionResponse(Session session)
    {
        var participants = session.Participants
            .Select(p => new ParticipantResponse(
                p.Id, p.Nickname, p.IsOrganizer, p.HasFinishedVoting, p.JoinedAt))
            .ToList();

        return new SessionResponse(
            session.Id,
            session.JoinCode,
            session.Destination,
            session.DestinationLat,
            session.DestinationLon,
            session.DurationDays,
            session.Status.ToString(),
            session.Language,
            session.CreatedAt,
            participants);
    }
}
