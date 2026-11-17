using AlignRoute.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AlignRoute.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Attraction> Attractions => Set<Attraction>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<Itinerary> Itineraries => Set<Itinerary>();
}