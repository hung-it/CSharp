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
    
    // Visit Tracking
    public DbSet<VisitSession> VisitSessions => Set<VisitSession>();
    public DbSet<TourViewSession> TourViewSessions => Set<TourViewSession>();
    public DbSet<PoiGeofenceEvent> PoiGeofenceEvents => Set<PoiGeofenceEvent>();
    
    // Shop Management
    public DbSet<ShopProfile> ShopProfiles => Set<ShopProfile>();
    public DbSet<ShopContent> ShopContents => Set<ShopContent>();
    public DbSet<ShopContentTranslation> ShopContentTranslations => Set<ShopContentTranslation>();
    public DbSet<ContentApprovalLog> ContentApprovalLogs => Set<ContentApprovalLog>();
    public DbSet<ShopAudioAsset> ShopAudioAssets => Set<ShopAudioAsset>();
    public DbSet<ShopTTSConfiguration> ShopTTSConfigurations => Set<ShopTTSConfiguration>();
    public DbSet<ShopQRCode> ShopQRCodes => Set<ShopQRCode>();
    public DbSet<ShopAnalyticsSnapshot> ShopAnalyticsSnapshots => Set<ShopAnalyticsSnapshot>();
    public DbSet<ShopLanguageReadiness> ShopLanguageReadiness => Set<ShopLanguageReadiness>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.ExternalRef).HasMaxLength(100);
            entity.Property(x => x.PreferredLanguage).HasMaxLength(10);
            entity.Property(x => x.Username).HasMaxLength(100);
            entity.Property(x => x.PasswordHash).HasMaxLength(256);
            entity.HasIndex(x => x.ExternalRef).IsUnique();
            entity.HasIndex(x => x.Username).IsUnique();
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

            // Relationship: POI có thể có ManagerUser (Shop Owner)
            entity.HasOne(p => p.ManagerUser)
                .WithMany()
                .HasForeignKey(p => p.ManagerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ShopProfile>(entity =>
        {
            entity.Property(x => x.ExternalRef).HasMaxLength(100);
            entity.HasIndex(x => x.ExternalRef);
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

        modelBuilder.Entity<VisitSession>(entity =>
        {
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.PoiId);
            entity.HasIndex(x => x.VisitedAtUtc);
            entity.HasIndex(x => new { x.PoiId, x.VisitedAtUtc });
            entity.HasOne(v => v.Poi)
                .WithMany(p => p.VisitSessions)
                .HasForeignKey(v => v.PoiId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TourViewSession>(entity =>
        {
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.TourId);
            entity.HasIndex(x => x.ViewedAtUtc);
            entity.HasOne(t => t.Tour)
                .WithMany()
                .HasForeignKey(t => t.TourId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PoiGeofenceEvent>(entity =>
        {
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.PoiId);
            entity.HasIndex(x => x.OccurredAtUtc);
            entity.HasIndex(x => new { x.PoiId, x.OccurredAtUtc });
            entity.HasOne(g => g.Poi)
                .WithMany(p => p.GeofenceEvents)
                .HasForeignKey(g => g.PoiId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShopQRCode>(entity =>
        {
            entity.HasOne(q => q.Poi)
                .WithMany()
                .HasForeignKey(q => q.PoiId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(q => q.Shop)
                .WithMany()
                .HasForeignKey(q => q.ShopId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ShopId, x.PoiId }).IsUnique();
        });
    }
}
