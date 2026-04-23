using VinhKhanhAudioGuide.Backend.Domain.Enums;

namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Username { get; set; }
    public required string ExternalRef { get; set; }
    public string? PasswordHash { get; set; }
    public string PreferredLanguage { get; set; } = "vi";
    public UserRole Role { get; set; } = UserRole.EndUser;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<UserEntitlement> Entitlements { get; set; } = new List<UserEntitlement>();
    public ICollection<ListeningSession> ListeningSessions { get; set; } = new List<ListeningSession>();
    public ICollection<RoutePoint> RoutePoints { get; set; } = new List<RoutePoint>();
}
