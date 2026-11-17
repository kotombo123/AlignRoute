namespace AlignRoute.Api.Models;

public class Session
{
    public int Id { get; set; }
    public string JoinCode { get; set; } = string.Empty; // np. "AB12CD"
    public string Destination { get; set; } = string.Empty;
    public int DurationDays { get; set; } = 3;
    public string Status { get; set; } = "Created"; // Created -> Exploring -> Voting -> Planning -> Completed
    public string Language { get; set; } = "pl";
    public List<Participant> Participants { get; set; } = new();
    public List<Attraction> Attractions { get; set; } = new();
}

public class Participant
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public bool IsOrganizer { get; set; }
    public bool HasFinishedVoting { get; set; }
    public virtual Session Session { get; set; }
}

public class Attraction
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ImageUrl { get; set; }
}

public class Vote
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int ParticipantId { get; set; }
    public int AttractionId { get; set; }
    public bool IsPositive { get; set; } // true = prawo, false = lewo
}

public class Itinerary
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string PlanJson { get; set; } = "{}";
}