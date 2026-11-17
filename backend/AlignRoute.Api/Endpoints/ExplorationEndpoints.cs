using AlignRoute.Api.Data;
using AlignRoute.Api.Hubs;
using AlignRoute.Api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AlignRoute.Api.Endpoints;

public static class ExplorationEndpoints
{
    public static void MapExplorationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sessions");

        // Uruchomienie AI-Eksploratora dla danej sesji
        group.MapPost("/{id:int}/explore", async (
            int id,
            AppDbContext db,
            ExplorerAgent explorer,
            IHubContext<SessionHub> hubContext) =>
        {
            var session = await db.Sessions.FindAsync(id);
            if (session is null)
                return Results.NotFound(new { error = "Nie znaleziono sesji." });

            session.Status = "Exploring";
            await db.SaveChangesAsync();

            var attractions = await explorer.GenerateAttractionsAsync(
                session.Id,
                session.Destination,
                session.Language);

            // Usuwamy ewentualne stare atrakcje z tej sesji i zapisujemy nowe
            var oldAttractions = await db.Attractions.Where(a => a.SessionId == id).ToListAsync();
            db.Attractions.RemoveRange(oldAttractions);

            db.Attractions.AddRange(attractions);
            session.Status = "Voting";
            await db.SaveChangesAsync();

            // Sygnał dla wszystkich uczestników w pokoju SignalR
            await hubContext.Clients.Group(id.ToString()).SendAsync("ExplorationComplete");

            return Results.Ok(attractions);
        });

        // Pobranie listy wygenerowanych atrakcji
        group.MapGet("/{id:int}/attractions", async (int id, AppDbContext db) =>
        {
            var attractions = await db.Attractions
                .Where(a => a.SessionId == id)
                .ToListAsync();

            return Results.Ok(attractions);
        });
    }
}