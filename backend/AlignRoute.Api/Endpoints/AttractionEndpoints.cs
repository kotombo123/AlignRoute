using AlignRoute.Api.Data;
using AlignRoute.Api.Dtos;
using AlignRoute.Api.Hubs;
using AlignRoute.Api.Models;
using AlignRoute.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AlignRoute.Api.Endpoints;

public static class AttractionEndpoints
{
    public static RouteGroupBuilder MapAttractionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{sessionId:guid}/explore", TriggerExploration);
        group.MapGet("/{sessionId:guid}/attractions", GetAttractions);
        return group;
    }

    private static async Task<IResult> TriggerExploration(
        Guid sessionId,
        AppDbContext db,
        IExplorerAgent explorerAgent,
        IHubContext<SessionHub> hubContext,
        ILogger<ExplorerAgent> logger,
        CancellationToken ct)
    {
        var session = await db.Sessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
            return Results.NotFound(new { error = "Session not found." });

        if (session.Status != SessionStatus.Created)
            return Results.BadRequest(new { error = "Exploration can only be started from Created status." });

        // Update status to Exploring
        db.Entry(session).Property(s => s.Status).CurrentValue = SessionStatus.Exploring;
        db.Entry(session).Property(s => s.UpdatedAt).CurrentValue = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        try
        {
            var attractions = await explorerAgent.ExploreAsync(session, ct);

            foreach (var attraction in attractions)
            {
                db.Attractions.Add(attraction);
            }

            // Update status to Voting
            db.Entry(session).Property(s => s.Status).CurrentValue = SessionStatus.Voting;
            db.Entry(session).Property(s => s.UpdatedAt).CurrentValue = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await hubContext.Clients.Group(sessionId.ToString())
                .SendAsync(SessionHubEvents.ExplorationComplete, attractions.Count, ct);

            return Results.Ok(new { attractionCount = attractions.Count, status = "Voting" });
        }
        catch (InvalidOperationException ex)
        {
            // Revert status on failure
            db.Entry(session).Property(s => s.Status).CurrentValue = SessionStatus.Created;
            db.Entry(session).Property(s => s.UpdatedAt).CurrentValue = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Json(
                new { error = $"Exploration failed: {ex.Message}" },
                statusCode: 502);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during exploration for session {SessionId}", sessionId);

            // Revert status on failure
            db.Entry(session).Property(s => s.Status).CurrentValue = SessionStatus.Created;
            db.Entry(session).Property(s => s.UpdatedAt).CurrentValue = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Json(
                new { error = "An unexpected error occurred during exploration. Please try again." },
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetAttractions(
        Guid sessionId,
        AppDbContext db,
        CancellationToken ct)
    {
        var sessionExists = await db.Sessions.AnyAsync(s => s.Id == sessionId, ct);
        if (!sessionExists)
            return Results.NotFound(new { error = "Session not found." });

        var attractions = await db.Attractions
            .Where(a => a.SessionId == sessionId)
            .Include(a => a.Votes)
            .OrderBy(a => a.SortOrder)
            .ToListAsync(ct);

        var response = attractions.Select(a => new AttractionResponse(
            a.Id,
            a.Name,
            a.Description,
            a.Category,
            a.Latitude,
            a.Longitude,
            a.ImageUrl,
            a.SortOrder,
            a.Votes.Count(v => v.IsPositive),
            a.Votes.Count(v => !v.IsPositive)
        )).ToList();

        return Results.Ok(response);
    }
}
