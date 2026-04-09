namespace VinhKhanhAudioGuide.Backend.Domain.Exceptions;

public class SubscriptionException : Exception
{
    public SubscriptionException(string message) : base(message) { }
    public SubscriptionException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class SubscriptionNotFoundException : SubscriptionException
{
    public SubscriptionNotFoundException(Guid userId)
        : base($"No active subscription found for user {userId}.") { }
}

public sealed class FeatureSegmentAccessDeniedException : SubscriptionException
{
    public FeatureSegmentAccessDeniedException(Guid userId, string featureSegmentCode)
        : base($"User {userId} does not have access to feature segment '{featureSegmentCode}'.") { }
}

public sealed class InvalidPlanTierException : SubscriptionException
{
    public InvalidPlanTierException(int planValue)
        : base($"Invalid plan tier value: {planValue}. Expected 1 (Basic) or 10 (PremiumSegmented).") { }
}
