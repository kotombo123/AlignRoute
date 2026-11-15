// AppDbContext.cs — kontekst bazy danych Entity Framework Core.
//
// Dodaj tutaj DbSet<T> dla każdego modelu który stworzysz.
// Domyślna baza to SQLite; connection string jest w appsettings.json ("DefaultConnection").
//
// Modele których potrzebuje frontend:
//   Session      — sesja grupowa (destination, durationDays, status, joinCode, language)
//   Participant  — uczestnik sesji (nickname, isOrganizer, hasFinishedVoting)
//   Attraction   — atrakcja (name, description, category, latitude, longitude, imageUrl)
//   Vote         — głos uczestnika na atrakcję (isPositive)
//   Itinerary    — wygenerowany plan jako JSON (planJson)
//
// Status sesji: Created → Exploring → Voting → Planning → Completed

using Microsoft.EntityFrameworkCore;

namespace AlignRoute.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // TODO: dodaj DbSety dla swoich modeli, np.:
    // public DbSet<Session> Sessions => Set<Session>();
    // public DbSet<Participant> Participants => Set<Participant>();
    // public DbSet<Attraction> Attractions => Set<Attraction>();
    // public DbSet<Vote> Votes => Set<Vote>();
    // public DbSet<Itinerary> Itineraries => Set<Itinerary>();
}
