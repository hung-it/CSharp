using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Domain.Enums;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface ISubscriptionService
{
    /// <summary>
    /// Gets the active subscription for a user.
    /// Throws SubscriptionNotFoundException if no active subscription exists.
    /// </summary>
    Task<Subscription> GetActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has an active subscription.
    /// </summary>
    Task<bool> HasActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has access to a specific feature segment.
    /// Returns true if: user has Basic tier (all basic features), OR has Premium tier AND explicit entitlement to segment.
    /// </summary>
    Task<bool> HasAccessToSegmentAsync(Guid userId, string featureSegmentCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a subscription plan for a user.
    /// If a previous subscription exists, it will be marked inactive.
    /// For Premium tier, feature segments will be granted automatically based on segment availability.
    /// </summary>
    Task<Subscription> ActivateSubscriptionAsync(Guid userId, PlanTier tier, decimal amountUsd, CancellationToken cancellationToken = default);

    /// <summary>
    /// Grants explicit access to a feature segment for a user (Premium tier).
    /// Throws if user doesn't have Premium subscription.
    /// </summary>
    Task GrantSegmentAccessAsync(Guid userId, string featureSegmentCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes access to a feature segment for a user.
    /// </summary>
    Task RevokeSegmentAccessAsync(Guid userId, string featureSegmentCode, CancellationToken cancellationToken = default);
}
