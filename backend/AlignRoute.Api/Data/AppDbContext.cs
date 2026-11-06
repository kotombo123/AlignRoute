using AlignRoute.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AlignRoute.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Attraction> Attractions => Set<Attraction>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<Itinerary> Itineraries => Set<Itinerary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JoinCode).IsUnique();
            entity.Property(e => e.JoinCode).HasMaxLength(6).IsRequired();
            entity.Property(e => e.Destination).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Language).HasMaxLength(5).HasDefaultValue("en");
            entity.Property(e => e.Status).HasConversion<int>();
        });

        modelBuilder.Entity<Participant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nickname).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ExcludedCategoriesJson).HasColumnName("ExcludedCategories");
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Participants)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Attraction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Category).HasMaxLength(50).IsRequired();
            entity.Property(e => e.OsmId).HasMaxLength(100);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Attractions)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Vote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AttractionId, e.ParticipantId }).IsUnique();
            entity.HasOne(e => e.Attraction)
                .WithMany(a => a.Votes)
                .HasForeignKey(e => e.AttractionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Participant)
                .WithMany(p => p.Votes)
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Itinerary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Session)
                .WithMany(s => s.Itineraries)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
