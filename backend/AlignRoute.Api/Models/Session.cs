namespace AlignRoute.Api.Models;

public sealed class Session
{
    public Guid Id { get; init; }
    public string JoinCode { get; init; } = string.Empty;
    public string Destination { get; init; } = string.Empty;
    public double DestinationLat { get; init; }
    public double DestinationLon { get; init; }
    public int DurationDays { get; init; }
    public SessionStatus Status { get; init; }
    public Guid OrganizerId { get; init; }
    public string Language { get; init; } = "en";
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public List<Participant> Participants { get; init; } = [];
    public List<Attraction> Attractions { get; init; } = [];
    public List<Itinerary> Itineraries { get; init; } = [];

    public Session WithStatus(SessionStatus status) =>
        new()
        {
            Id = Id,
            JoinCode = JoinCode,
            Destination = Destination,
            DestinationLat = DestinationLat,
            DestinationLon = DestinationLon,
            DurationDays = DurationDays,
            Status = status,
            OrganizerId = OrganizerId,
            Language = Language,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            Participants = Participants,
            Attractions = Attractions,
            Itineraries = Itineraries
        };
}
