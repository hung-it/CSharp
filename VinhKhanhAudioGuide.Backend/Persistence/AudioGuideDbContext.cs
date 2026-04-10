using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Entities;

namespace VinhKhanhAudioGuide.Backend.Persistence;

public sealed class AudioGuideDbContext(DbContextOptions<AudioGuideDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<FeatureSegment> FeatureSegments => Set<FeatureSegment>();
    public DbSet<UserEntitlement> UserEntitlements => Set<UserEntitlement>();
    public DbSet<Poi> Pois => Set<Poi>();
    public DbSet<AudioAsset> AudioAssets => Set<AudioAsset>();
    public DbSet<ContentTranslation> ContentTranslations => Set<ContentTranslation>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<TourStop> TourStops => Set<TourStop>();
    public DbSet<ListeningSession> ListeningSessions => Set<ListeningSession>();
    public DbSet<RoutePoint> RoutePoints => Set<RoutePoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.ExternalRef).HasMaxLength(100);
            entity.Property(x => x.PreferredLanguage).HasMaxLength(10);
            entity.HasIndex(x => x.ExternalRef).IsUnique();
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.Property(x => x.AmountUsd).HasPrecision(10, 2);
            entity.HasIndex(x => new { x.UserId, x.IsActive });
            entity.HasIndex(x => x.UserId)
                .HasFilter("\"IsActive\" = 1")
                .IsUnique();
        });

        modelBuilder.Entity<FeatureSegment>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(100);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<UserEntitlement>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.FeatureSegmentId }).IsUnique();
        });

        modelBuilder.Entity<Poi>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(100);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.District).HasMaxLength(100);
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.Property(x => x.MapLink).HasMaxLength(500);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<AudioAsset>(entity =>
        {
            entity.Property(x => x.LanguageCode).HasMaxLength(10);
            entity.Property(x => x.FilePath).HasMaxLength(400);
            entity.HasIndex(x => new { x.PoiId, x.LanguageCode }).IsUnique();
        });

        modelBuilder.Entity<ContentTranslation>(entity =>
        {
            entity.Property(x => x.ContentKey).HasMaxLength(150);
            entity.Property(x => x.LanguageCode).HasMaxLength(10);
            entity.HasIndex(x => new { x.ContentKey, x.LanguageCode }).IsUnique();
        });

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(100);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<TourStop>(entity =>
        {
            entity.HasIndex(x => new { x.TourId, x.Sequence }).IsUnique();
            entity.HasIndex(x => new { x.TourId, x.PoiId });
        });

        modelBuilder.Entity<ListeningSession>(entity =>
        {
            entity.HasIndex(x => x.PoiId);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.StartedAtUtc);
        });

        modelBuilder.Entity<RoutePoint>(entity =>
        {
            entity.Property(x => x.Source).HasMaxLength(50);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.RecordedAtUtc);
        });
    }
}
