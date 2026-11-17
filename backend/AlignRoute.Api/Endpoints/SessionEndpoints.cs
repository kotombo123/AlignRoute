using AlignRoute.Api.Data;
using AlignRoute.Api.Hubs;
using AlignRoute.Api.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace AlignRoute.Api.Endpoints;

public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sessions");

        group.MapPost("/", async ([FromBody] CreateSessionRequest req, [FromServices] AppDbContext db) =>
        {
            var joinCode = GenerateJoinCode();

            var session = new Session
            {
                Destination = req.Destination,
                DurationDays = req.DurationDays,
                Language = req.Language,
                JoinCode = joinCode,
                Status = "Created"
            };

            var host = new Participant
            {
                Nickname = req.OrganizerNickname,
                IsOrganizer = true,
                Session = session,
                HasFinishedVoting = false
            };

            db.Sessions.Add(session);
            db.Participants.Add(host);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                sessionId = session.Id,
                joinCode = session.JoinCode,
                participantId = host.Id
            });
        });

        group.MapGet("/{code}", async (string code, [FromServices] AppDbContext db) =>
        {
            var session = await db.Sessions
                .Include(s => s.Participants)
                .Include(s => s.Attractions)
                .FirstOrDefaultAsync(s => s.JoinCode == code);

            return session is null ? Results.NotFound() : Results.Ok(session);
        });

        group.MapGet("/{id:int}/status", async (int id, [FromServices] AppDbContext db) =>
        {
            var session = await db.Sessions
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session is null) return Results.NotFound();

            return Results.Ok(new 
            { 
                status = session.Status, 
                participants = session.Participants 
            });
        });

        group.MapGet("/{id:int}/cards/{participantId:int}", async (int id, int participantId, [FromServices] AppDbContext db) =>
        {
            var attractions = await db.Attractions
                .Where(a => a.SessionId == id)
                .ToListAsync();

            return Results.Ok(attractions);
        });

        group.MapPost("/{code}/join", async (string code, [FromBody] JoinSessionRequest req, [FromServices] AppDbContext db, [FromServices] IHubContext<SessionHub> hubContext) =>
        {
            var session = await db.Sessions.FirstOrDefaultAsync(s => s.JoinCode == code);
            if (session is null) return Results.NotFound("Session not found.");

            var participant = new Participant
            {
                Nickname = req.Nickname,
                IsOrganizer = false,
                SessionId = session.Id,
                HasFinishedVoting = false
            };

            db.Participants.Add(participant);
            await db.SaveChangesAsync();

            await hubContext.Clients.Group(session.Id.ToString())
                .SendAsync("ParticipantJoined", new { participantId = participant.Id, nickname = participant.Nickname });

            return Results.Ok(new
            {
                sessionId = session.Id,
                participantId = participant.Id
            });
        });

        group.MapPost("/{id:int}/vote", async (int id, [FromBody] VoteRequest req, [FromServices] AppDbContext db, [FromServices] IHubContext<SessionHub> hubContext) =>
        {
            var vote = await db.Votes
                .FirstOrDefaultAsync(v => v.ParticipantId == req.ParticipantId && v.AttractionId == req.AttractionId);

            if (vote != null)
            {
                vote.IsPositive = req.IsPositive;
            }
            else
            {
                db.Votes.Add(new Vote
                {
                    ParticipantId = req.ParticipantId,
                    AttractionId = req.AttractionId,
                    IsPositive = req.IsPositive
                });
            }

            await db.SaveChangesAsync();

            await hubContext.Clients.Group(id.ToString())
                .SendAsync("VoteUpdate", new { participantId = req.ParticipantId, attractionId = req.AttractionId });

            return Results.Ok();
        });

        group.MapPut("/{sessionId:int}/participants/{participantId:int}/done", async (int sessionId, int participantId, [FromServices] AppDbContext db, [FromServices] IHubContext<SessionHub> hubContext) =>
        {
            var participant = await db.Participants
                .FirstOrDefaultAsync(p => p.Id == participantId && p.SessionId == sessionId);
            
            if (participant == null) return Results.NotFound();

            participant.HasFinishedVoting = true;
            await db.SaveChangesAsync();

            await hubContext.Clients.Group(sessionId.ToString())
                .SendAsync("ParticipantFinished", new { participantId = participant.Id });

            return Results.Ok();
        });

        group.MapPost("/{id:int}/generate-plan", async (int id, [FromServices] AppDbContext db, [FromServices] IHubContext<SessionHub> hubContext) =>
        {
            var session = await db.Sessions.FindAsync(id);
            if (session is null) return Results.NotFound();

            session.Status = "Completed";

            var mockItinerary = new Itinerary
            {
                SessionId = id,
                PlanJson = "{ \"message\": \"To jest mockowy wygenerowany plan. Integracja z LLM w drodze!\" }"
            };

            db.Itineraries.Add(mockItinerary);
            await db.SaveChangesAsync();

            await hubContext.Clients.Group(id.ToString())
                .SendAsync("PlanGenerated", new { itineraryId = mockItinerary.Id });

            return Results.Ok();
        });

        group.MapGet("/{id:int}/itinerary", async (int id, [FromServices] AppDbContext db) =>
        {
            var itinerary = await db.Itineraries.FirstOrDefaultAsync(i => i.SessionId == id);
            return itinerary is null ? Results.NotFound() : Results.Ok(itinerary);
        });
    }

    private static string GenerateJoinCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }
}

public record CreateSessionRequest(string Destination, int DurationDays, string Language, string OrganizerNickname);
public record JoinSessionRequest(string Nickname);
public record VoteRequest(int ParticipantId, int AttractionId, bool IsPositive);