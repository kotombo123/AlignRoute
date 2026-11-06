using System.Text.Json;
using AlignRoute.Api.Data;
using AlignRoute.Api.Dtos;
using AlignRoute.Api.Hubs;
using AlignRoute.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AlignRoute.Api.Endpoints;

public static class VotingEndpoints
{
    public static RouteGroupBuilder MapVotingEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{sessionId:guid}/cards/{participantId:guid}", GetVotingCards);
        group.MapPost("/{sessionId:guid}/vote", CastVote);
        group.MapPut("/{sessionId:guid}/participants/{participantId:guid}/exclusions", UpdateExclusions);
        group.MapPut("/{sessionId:guid}/participants/{participantId:guid}/done", MarkVotingDone);
        return group;
    }

    private static async Task<IResult> GetVotingCards(
        Guid sessionId,
        Guid participantId,
        AppDbContext db,
        CancellationToken ct)
    {
        var participant = await db.Participants
            .FirstOrDefaultAsync(p => p.Id == participantId && p.SessionId == sessionId, ct);

        if (participant is null)
            return Results.NotFound(new { error = "Participant not found in this session." });

        var excludedCategories = DeserializeExclusions(participant.ExcludedCategoriesJson);

        var votedAttractionIds = await db.Votes
            .Where(v => v.ParticipantId == participantId)
            .Select(v => v.AttractionId)
            .ToListAsync(ct);

        var cards = await db.Attractions
            .Where(a => a.SessionId == sessionId)
            .Where(a => !votedAttractionIds.Contains(a.Id))
            .Where(a => !excludedCategories.Contains(a.Category.ToLower()))
            .OrderBy(a => a.SortOrder)
            .Select(a => new VoteCardResponse(
                a.Id, a.Name, a.Description, a.Category,
                a.Latitude, a.Longitude, a.ImageUrl, a.SortOrder))
            .ToListAsync(ct);

        return Results.Ok(cards);
    }

    private static async Task<IResult> CastVote(
        Guid sessionId,
        CastVoteRequest request,
        AppDbContext db,
        IHubContext<SessionHub> hubContext,
        CancellationToken ct)
    {
        var participant = await db.Participants
            .FirstOrDefaultAsync(p => p.Id == request.ParticipantId && p.SessionId == sessionId, ct);

        if (participant is null)
            return Results.NotFound(new { error = "Participant not found in this session." });

        var attraction = await db.Attractions
            .FirstOrDefaultAsync(a => a.Id == request.AttractionId && a.SessionId == sessionId, ct);

        if (attraction is null)
            return Results.NotFound(new { error = "Attraction not found in this session." });

        var existingVote = await db.Votes
            .FirstOrDefaultAsync(v =>
                v.AttractionId == request.AttractionId && v.ParticipantId == request.ParticipantId, ct);

        if (existingVote is not null)
            return Results.Conflict(new { error = "You have already voted on this attraction." });

        var vote = new Vote
        {
            Id = Guid.NewGuid(),
            AttractionId = request.AttractionId,
            ParticipantId = request.ParticipantId,
            IsPositive = request.IsPositive,
            VotedAt = DateTime.UtcNow
        };

        db.Votes.Add(vote);
        await db.SaveChangesAsync(ct);

        // Calculate updated counts
        var yesCount = await db.Votes
            .CountAsync(v => v.AttractionId == request.AttractionId && v.IsPositive, ct);
        var noCount = await db.Votes
            .CountAsync(v => v.AttractionId == request.AttractionId && !v.IsPositive, ct);

        await hubContext.Clients.Group(sessionId.ToString())
            .SendAsync(SessionHubEvents.VoteUpdate, request.AttractionId, yesCount, noCount, ct);

        return Results.Ok(new { attractionId = request.AttractionId, yesCount, noCount });
    }

    private static async Task<IResult> UpdateExclusions(
        Guid sessionId,
        Guid participantId,
        UpdateExclusionsRequest request,
        AppDbContext db,
        CancellationToken ct)
    {
        var participant = await db.Participants
            .FirstOrDefaultAsync(p => p.Id == participantId && p.SessionId == sessionId, ct);

        if (participant is null)
            return Results.NotFound(new { error = "Participant not found in this session." });

        var exclusionsJson = JsonSerializer.Serialize(
            request.ExcludedCategories.Select(c => c.ToLowerInvariant()).Distinct().ToList());

        db.Entry(participant).Property(p => p.ExcludedCategoriesJson).CurrentValue = exclusionsJson;
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { excludedCategories = request.ExcludedCategories });
    }

    private static async Task<IResult> MarkVotingDone(
        Guid sessionId,
        Guid participantId,
        AppDbContext db,
        IHubContext<SessionHub> hubContext,
        CancellationToken ct)
    {
        var participant = await db.Participants
            .FirstOrDefaultAsync(p => p.Id == participantId && p.SessionId == sessionId, ct);

        if (participant is null)
            return Results.NotFound(new { error = "Participant not found in this session." });

        db.Entry(participant).Property(p => p.HasFinishedVoting).CurrentValue = true;
        await db.SaveChangesAsync(ct);

        await hubContext.Clients.Group(sessionId.ToString())
            .SendAsync(SessionHubEvents.ParticipantFinished, participant.Nickname, ct);

        return Results.Ok(new { participantId, hasFinishedVoting = true });
    }

    private static List<string> DeserializeExclusions(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
