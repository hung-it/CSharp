using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Domain.Enums;
using VinhKhanhAudioGuide.Backend.Domain.Exceptions;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public sealed class SubscriptionService(AudioGuideDbContext dbContext) : ISubscriptionService
{
    private readonly AudioGuideDbContext _dbContext = dbContext;

    public async Task<Subscription> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var subscription = await _dbContext.Subscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            throw new SubscriptionNotFoundException(userId);
        }

        return subscription;
    }

    public async Task<bool> HasActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Subscriptions
            .AnyAsync(s => s.UserId == userId && s.IsActive, cancellationToken);
    }

    public async Task<bool> HasAccessToSegmentAsync(Guid userId, string featureSegmentCode, CancellationToken cancellationToken = default)
    {
        var subscription = await _dbContext.Subscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            return false;
        }

        if (subscription.PlanTier == PlanTier.Basic)
        {
            return featureSegmentCode.StartsWith("basic.");
        }

        if (subscription.PlanTier == PlanTier.PremiumSegmented)
        {
            if (featureSegmentCode.StartsWith("basic.", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var entitlement = await _dbContext.UserEntitlements
                .Include(e => e.FeatureSegment)
                .Where(e => e.UserId == userId && e.FeatureSegment!.Code == featureSegmentCode && e.RevokedAtUtc == null)
                .FirstOrDefaultAsync(cancellationToken);

            return entitlement is not null;
        }

        return false;
    }

    public async Task<Subscription> ActivateSubscriptionAsync(Guid userId, PlanTier tier, decimal amountUsd, CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? transaction = null;
        if (_dbContext.Database.IsRelational())
        {
            transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var existingSubscription = await _dbContext.Subscriptions
                .Where(s => s.UserId == userId && s.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingSubscription is not null)
            {
                existingSubscription.IsActive = false;
            }

            var newSubscription = new Subscription
            {
                UserId = userId,
                PlanTier = tier,
                AmountUsd = amountUsd,
                IsActive = true,
                ActivatedAtUtc = DateTime.UtcNow
            };

            _dbContext.Subscriptions.Add(newSubscription);

            if (tier == PlanTier.PremiumSegmented)
            {
                var allSegments = await _dbContext.FeatureSegments.ToListAsync(cancellationToken);
                var premiumSegments = allSegments.Where(s => s.Code.StartsWith("premium.")).ToList();

                var newEntitlements = premiumSegments.Select(segment => new UserEntitlement
                {
                    UserId = userId,
                    FeatureSegmentId = segment.Id,
                    GrantedAtUtc = DateTime.UtcNow
                });

                _dbContext.UserEntitlements.AddRange(newEntitlements);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return newSubscription;
        }
        catch (DbUpdateException ex)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            throw new SubscriptionException("Subscription activation conflicted with another operation. Please retry.", ex);
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task GrantSegmentAccessAsync(Guid userId, string featureSegmentCode, CancellationToken cancellationToken = default)
    {
        var subscription = await _dbContext.Subscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null || subscription.PlanTier != PlanTier.PremiumSegmented)
        {
            throw new SubscriptionException($"User {userId} does not have a Premium subscription to grant segment access.");
        }

        var segment = await _dbContext.FeatureSegments
            .FirstOrDefaultAsync(s => s.Code == featureSegmentCode, cancellationToken);

        if (segment is null)
        {
            throw new SubscriptionException($"Feature segment '{featureSegmentCode}' does not exist.");
        }

        var existingEntitlement = await _dbContext.UserEntitlements
            .FirstOrDefaultAsync(e => e.UserId == userId && e.FeatureSegmentId == segment.Id, cancellationToken);

        if (existingEntitlement is not null && existingEntitlement.RevokedAtUtc is null)
        {
            return;
        }

        if (existingEntitlement is not null)
        {
            existingEntitlement.RevokedAtUtc = null;
        }
        else
        {
            var newEntitlement = new UserEntitlement
            {
                UserId = userId,
                FeatureSegmentId = segment.Id,
                GrantedAtUtc = DateTime.UtcNow
            };
            _dbContext.UserEntitlements.Add(newEntitlement);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeSegmentAccessAsync(Guid userId, string featureSegmentCode, CancellationToken cancellationToken = default)
    {
        var segment = await _dbContext.FeatureSegments
            .FirstOrDefaultAsync(s => s.Code == featureSegmentCode, cancellationToken);

        if (segment is null)
        {
            return;
        }

        var entitlement = await _dbContext.UserEntitlements
            .FirstOrDefaultAsync(e => e.UserId == userId && e.FeatureSegmentId == segment.Id && e.RevokedAtUtc == null, cancellationToken);

        if (entitlement is not null)
        {
            entitlement.RevokedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
