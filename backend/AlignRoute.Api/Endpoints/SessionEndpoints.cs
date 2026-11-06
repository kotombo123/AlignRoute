using AlignRoute.Api.Data;
using AlignRoute.Api.Dtos;
using AlignRoute.Api.Hubs;
using AlignRoute.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AlignRoute.Api.Endpoints;

public static class SessionEndpoints
{
    public static RouteGroupBuilder MapSessionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateSession);
        group.MapGet("/{joinCode}", GetSessionByJoinCode);
        group.MapPost("/{joinCode}/join", JoinSession);
        group.MapGet("/{sessionId:guid}/status", GetSessionStatus);
        return group;
    }

    private static async Task<IResult> CreateSession(
        CreateSessionRequest request,
        AppDbContext db,
        IConfiguration config,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Destination))
            return Results.BadRequest(new { error = "Destination is required." });

        if (request.DurationDays < 1 || request.DurationDays > 7)
            return Results.BadRequest(new { error = "Duration must be between 1 and 7 days." });

        if (string.IsNullOrWhiteSpace(request.OrganizerNickname))
            return Results.BadRequest(new { error = "Organizer nickname is required." });

        var sessionId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var joinCode = GenerateJoinCode();
        var now = DateTime.UtcNow;

        var session = new Session
        {
            Id = sessionId,
            JoinCode = joinCode,
            Destination = request.Destination.Trim(),
            DestinationLat = 0,
            DestinationLon = 0,
            DurationDays = request.DurationDays,
            Status = SessionStatus.Created,
            OrganizerId = organizerId,
            Language = request.Language ?? "en",
            CreatedAt = now,
            UpdatedAt = now
        };

        var organizer = new Participant
        {
            Id = organizerId,
            SessionId = sessionId,
            Nickname = request.OrganizerNickname.Trim(),
            IsOrganizer = true,
            ExcludedCategoriesJson = "[]",
            HasFinishedVoting = false,
            JoinedAt = now
        };

        db.Sessions.Add(session);
        db.Participants.Add(organizer);
        await db.SaveChangesAsync(ct);

        var loadedSession = await db.Sessions
            .Include(s => s.Participants)
            .FirstAsync(s => s.Id == sessionId, ct);

        var response = new CreateSessionResponse(
            sessionId,
            joinCode,
            organizerId,
            ResponseMapper.ToSessionResponse(loadedSession));

        return Results.Created($"/api/sessions/{joinCode}", response);
    }

    private static async Task<IResult> GetSessionByJoinCode(
        string joinCode,
        AppDbContext db,
        CancellationToken ct)
    {
        var session = await db.Sessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.JoinCode == joinCode.ToUpperInvariant(), ct);

        if (session is null)
            return Results.NotFound(new { error = "Session not found." });

        return Results.Ok(ResponseMapper.ToSessionResponse(session));
    }

    private static async Task<IResult> JoinSession(
        string joinCode,
        JoinSessionRequest request,
        AppDbContext db,
        IConfiguration config,
        IHubContext<SessionHub> hubContext,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Nickname))
            return Results.BadRequest(new { error = "Nickname is required." });

        var session = await db.Sessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.JoinCode == joinCode.ToUpperInvariant(), ct);

        if (session is null)
            return Results.NotFound(new { error = "Session not found." });

        var maxParticipants = config.GetValue("SessionSettings:MaxParticipants", 20);
        if (session.Participants.Count >= maxParticipants)
            return Results.BadRequest(new { error = $"Session is full (max {maxParticipants} participants)." });

        var nickname = request.Nickname.Trim();
        var alreadyJoined = session.Participants.Any(p =>
            p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));
        if (alreadyJoined)
            return Results.Conflict(new { error = "A participant with this nickname already exists." });

        var participantId = Guid.NewGuid();
        var participant = new Participant
        {
            Id = participantId,
            SessionId = session.Id,
            Nickname = nickname,
            IsOrganizer = false,
            ExcludedCategoriesJson = "[]",
            HasFinishedVoting = false,
            JoinedAt = DateTime.UtcNow
        };

        db.Participants.Add(participant);
        await db.SaveChangesAsync(ct);

        await hubContext.Clients.Group(session.Id.ToString())
            .SendAsync(SessionHubEvents.ParticipantJoined,
                new { participantId = participantId.ToString(), nickname = participant.Nickname }, ct);

        var reloaded = await db.Sessions
            .Include(s => s.Participants)
            .FirstAsync(s => s.Id == session.Id, ct);

        return Results.Ok(new JoinSessionResponse(
            participantId,
            ResponseMapper.ToSessionResponse(reloaded)));
    }

    private static async Task<IResult> GetSessionStatus(
        Guid sessionId,
        AppDbContext db,
        CancellationToken ct)
    {
        var session = await db.Sessions
            .Include(s => s.Participants)
            .Include(s => s.Attractions)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
            return Results.NotFound(new { error = "Session not found." });

        var totalVotes = await db.Votes
            .CountAsync(v => v.Attraction!.SessionId == sessionId, ct);

        var participants = session.Participants
            .Select(p => new ParticipantResponse(
                p.Id, p.Nickname, p.IsOrganizer, p.HasFinishedVoting, p.JoinedAt))
            .ToList();

        return Results.Ok(new StatusResponse(
            session.Status.ToString(),
            participants,
            session.Attractions.Count,
            totalVotes));
    }

    private static string GenerateJoinCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = Random.Shared;
        return new string(Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
