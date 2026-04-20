using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VinhKhanhAudioGuide.Backend.Application.Services;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Domain.Enums;
using VinhKhanhAudioGuide.Backend.Domain.Exceptions;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Tests.Application.Services;

public sealed class SubscriptionServiceTests
{
    private AudioGuideDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AudioGuideDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AudioGuideDbContext(options);
    }


    [Fact]
    public async Task GetActiveSubscriptionAsync_ThrowsWhenNoActiveSubscription()
    {
        var dbContext = CreateDbContext();
        var service = new SubscriptionService(dbContext);
        var userId = Guid.NewGuid();

        await Assert.ThrowsAsync<SubscriptionNotFoundException>(() => service.GetActiveSubscriptionAsync(userId));
    }

    [Fact]
    public async Task GetActiveSubscriptionAsync_ReturnsActiveSubscription()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var subscription = new Subscription
        {
            UserId = userId,
            PlanTier = PlanTier.Basic,
            AmountUsd = 1m,
            IsActive = true
        };
        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        var service = new SubscriptionService(dbContext);
        var result = await service.GetActiveSubscriptionAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(PlanTier.Basic, result.PlanTier);
    }

    [Fact]
    public async Task HasActiveSubscriptionAsync_ReturnsFalseWhenNoSubscription()
    {
        var dbContext = CreateDbContext();
        var service = new SubscriptionService(dbContext);
        var userId = Guid.NewGuid();

        var result = await service.HasActiveSubscriptionAsync(userId);

        Assert.False(result);
    }

    [Fact]
    public async Task HasActiveSubscriptionAsync_ReturnsTrueWhenActive()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var subscription = new Subscription
        {
            UserId = userId,
            PlanTier = PlanTier.Basic,
            AmountUsd = 1m,
            IsActive = true
        };
        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        var service = new SubscriptionService(dbContext);
        var result = await service.HasActiveSubscriptionAsync(userId);

        Assert.True(result);
    }

    [Fact]
    public async Task HasAccessToSegmentAsync_ReturnsFalseWhenNoSubscription()
    {
        var dbContext = CreateDbContext();
        var service = new SubscriptionService(dbContext);
        var userId = Guid.NewGuid();

        var result = await service.HasAccessToSegmentAsync(userId, "basic.poi");

        Assert.False(result);
    }

    [Fact]
    public async Task HasAccessToSegmentAsync_AllowsBasicSegmentsForBasicTier()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var subscription = new Subscription
        {
            UserId = userId,
            PlanTier = PlanTier.Basic,
            AmountUsd = 1m,
            IsActive = true
        };
        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        var service = new SubscriptionService(dbContext);
        var result = await service.HasAccessToSegmentAsync(userId, "basic.poi");

        Assert.True(result);
    }

    [Fact]
    public async Task HasAccessToSegmentAsync_AllowsBasicSegmentsForPremiumTier()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var subscription = new Subscription
        {
            UserId = userId,
            PlanTier = PlanTier.PremiumSegmented,
            AmountUsd = 10m,
            IsActive = true
        };
        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        var service = new SubscriptionService(dbContext);
        var result = await service.HasAccessToSegmentAsync(userId, "basic.poi");

        Assert.True(result);
    }

    [Fact]
    public async Task HasAccessToSegmentAsync_DeniesPremiumSegmentsForPremiumTierWithoutEntitlement()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var subscription = new Subscription
        {
            UserId = userId,
            PlanTier = PlanTier.PremiumSegmented,
            AmountUsd = 10m,
            IsActive = true
        };
        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        var service = new SubscriptionService(dbContext);
        var result = await service.HasAccessToSegmentAsync(userId, "premium.segment.tour");

        Assert.False(result);
    }

    [Fact]
    public async Task ActivateSubscriptionAsync_CreatesBasicSubscription()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var service = new SubscriptionService(dbContext);

        var result = await service.ActivateSubscriptionAsync(userId, PlanTier.Basic, 1m);

        Assert.NotNull(result);
        Assert.Equal(PlanTier.Basic, result.PlanTier);
        Assert.Equal(1m, result.AmountUsd);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task ActivateSubscriptionAsync_CreatesPremiumAndGrantsAllSegments()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();

        var segment1 = new FeatureSegment { Code = "premium.segment.tour", Name = "Tour" };
        var segment2 = new FeatureSegment { Code = "premium.segment.audio", Name = "Audio" };
        var segment3 = new FeatureSegment { Code = "basic.poi", Name = "Basic" };

        dbContext.FeatureSegments.AddRange(segment1, segment2, segment3);
        await dbContext.SaveChangesAsync();

        var service = new SubscriptionService(dbContext);
        var result = await service.ActivateSubscriptionAsync(userId, PlanTier.PremiumSegmented, 10m);

        Assert.Equal(PlanTier.PremiumSegmented, result.PlanTier);

        var entitlements = await dbContext.UserEntitlements
            .Where(e => e.UserId == userId && e.RevokedAtUtc == null)
            .CountAsync();

        Assert.Equal(2, entitlements);
    }

    [Fact]
    public async Task ActivateSubscriptionAsync_DeactivatesPreviousSubscription()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();

        var oldSubscription = new Subscription
        {
            UserId = userId,
            PlanTier = PlanTier.Basic,
            AmountUsd = 1m,
            IsActive = true
        };
        dbContext.Subscriptions.Add(oldSubscription);
        await dbContext.SaveChangesAsync();

        var service = new SubscriptionService(dbContext);
        await service.ActivateSubscriptionAsync(userId, PlanTier.PremiumSegmented, 10m);

        var reloadedOld = await dbContext.Subscriptions.FindAsync(oldSubscription.Id);
        Assert.False(reloadedOld!.IsActive);
    }

    [Fact]
    public async Task GrantSegmentAccessAsync_GrantsAccessWhenPremiumTier()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();

        var segment = new FeatureSegment { Code = "premium.segment.tour", Name = "Tour" };
        dbContext.FeatureSegments.Add(segment);

        var subscription = new Subscription
        {
            UserId = userId,
            PlanTier = PlanTier.PremiumSegmented,
            AmountUsd = 10m,
            IsActive = true
        };
        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        var service = new SubscriptionService(dbContext);
        await service.GrantSegmentAccessAsync(userId, "premium.segment.tour");

        var entitlement = await dbContext.UserEntitlements
            .FirstOrDefaultAsync(e => e.UserId == userId && e.FeatureSegment!.Code == "premium.segment.tour");

        Assert.NotNull(entitlement);
    }

    [Fact]
    public async Task GrantSegmentAccessAsync_ThrowsWhenNoActiveSubscription()
    {
        var dbContext = CreateDbContext();
        var service = new SubscriptionService(dbContext);
        var userId = Guid.NewGuid();

        await Assert.ThrowsAsync<SubscriptionException>(() => service.GrantSegmentAccessAsync(userId, "premium.segment.tour"));
    }

    [Fact]
    public async Task RevokeSegmentAccessAsync_RevokesExistingEntitlement()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();

        var segment = new FeatureSegment { Code = "premium.segment.tour", Name = "Tour" };
        dbContext.FeatureSegments.Add(segment);

        var entitlement = new UserEntitlement
        {
            UserId = userId,
            FeatureSegmentId = segment.Id,
            GrantedAtUtc = DateTime.UtcNow
        };
        dbContext.UserEntitlements.Add(entitlement);
        await dbContext.SaveChangesAsync();

        var service = new SubscriptionService(dbContext);
        await service.RevokeSegmentAccessAsync(userId, "premium.segment.tour");

        var reloadedEntitlement = await dbContext.UserEntitlements.FindAsync(entitlement.Id);
        Assert.NotNull(reloadedEntitlement!.RevokedAtUtc);
    }
}
