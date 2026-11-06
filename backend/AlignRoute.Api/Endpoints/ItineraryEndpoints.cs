using AlignRoute.Api.Data;
using AlignRoute.Api.Hubs;
using AlignRoute.Api.Models;
using AlignRoute.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AlignRoute.Api.Endpoints;

public static class ItineraryEndpoints
{
    public static RouteGroupBuilder MapItineraryEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{sessionId:guid}/generate-plan", GeneratePlan);
        group.MapGet("/{sessionId:guid}/itinerary", GetItinerary);
        return group;
    }

    private static async Task<IResult> GeneratePlan(
        Guid sessionId,
        AppDbContext db,
        IPlannerAgent plannerAgent,
        IHubContext<SessionHub> hubContext,
        ILogger<PlannerAgent> logger,
        CancellationToken ct)
    {
        var session = await db.Sessions
            .Include(s => s.Participants)
            .Include(s => s.Attractions)
                .ThenInclude(a => a.Votes)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
            return Results.NotFound(new { error = "Session not found." });

        if (session.Status != SessionStatus.Voting)
            return Results.BadRequest(new { error = "Plan can only be generated from Voting status." });

        // Determine winning attractions: YES > NO votes
        var winningAttractions = session.Attractions
            .Where(a =>
            {
                var yesCount = a.Votes.Count(v => v.IsPositive);
                var noCount = a.Votes.Count(v => !v.IsPositive);
                return yesCount > noCount;
            })
            .ToList();

        if (winningAttractions.Count == 0)
        {
            return Results.BadRequest(new
            {
                error = "No attractions received enough positive votes to include in the plan."
            });
        }

        // Update status to Planning
        db.Entry(session).Property(s => s.Status).CurrentValue = SessionStatus.Planning;
        db.Entry(session).Property(s => s.UpdatedAt).CurrentValue = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        try
        {
            var planJson = await plannerAgent.GeneratePlanAsync(session, winningAttractions, ct);

            var itinerary = new Itinerary
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                GeneratedAt = DateTime.UtcNow,
                PlanJson = planJson
            };

            db.Itineraries.Add(itinerary);

            // Update status to Completed
            db.Entry(session).Property(s => s.Status).CurrentValue = SessionStatus.Completed;
            db.Entry(session).Property(s => s.UpdatedAt).CurrentValue = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await hubContext.Clients.Group(sessionId.ToString())
                .SendAsync(SessionHubEvents.PlanGenerated, itinerary.Id, ct);

            return Results.Ok(new
            {
                itineraryId = itinerary.Id,
                plan = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(planJson),
                status = "Completed"
            });
        }
        catch (InvalidOperationException ex)
        {
            // Revert status on failure
            db.Entry(session).Property(s => s.Status).CurrentValue = SessionStatus.Voting;
            db.Entry(session).Property(s => s.UpdatedAt).CurrentValue = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Json(
                new { error = ex.Message },
                statusCode: 502);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during plan generation for session {SessionId}", sessionId);

            // Revert status on failure
            db.Entry(session).Property(s => s.Status).CurrentValue = SessionStatus.Voting;
            db.Entry(session).Property(s => s.UpdatedAt).CurrentValue = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Json(
                new { error = "An unexpected error occurred while generating the plan. Please try again." },
                statusCode: 500);
        }
    }

    private static async Task<IResult> GetItinerary(
        Guid sessionId,
        AppDbContext db,
        CancellationToken ct)
    {
        var itinerary = await db.Itineraries
            .Where(i => i.SessionId == sessionId)
            .OrderByDescending(i => i.GeneratedAt)
            .FirstOrDefaultAsync(ct);

        if (itinerary is null)
            return Results.NotFound(new { error = "No itinerary found for this session." });

        return Results.Ok(new
        {
            itinerary.Id,
            itinerary.SessionId,
            itinerary.GeneratedAt,
            plan = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(itinerary.PlanJson)
        });
    }
}
