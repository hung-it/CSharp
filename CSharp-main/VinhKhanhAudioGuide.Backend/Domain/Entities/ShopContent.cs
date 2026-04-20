namespace VinhKhanhAudioGuide.Backend.Domain.Entities;

public sealed class ShopContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShopId { get; set; }
    public Guid PoiId { get; set; }
    public string? TextScript { get; set; }
    public ContentApprovalStatus ApprovalStatus { get; set; } = ContentApprovalStatus.Draft;
    public DateTime LastModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? RejectionReason { get; set; }

    public ShopProfile? Shop { get; set; }
    public Poi? Poi { get; set; }
    public ICollection<ShopContentTranslation> Translations { get; set; } = new List<ShopContentTranslation>();
    public ICollection<ContentApprovalLog> ApprovalLogs { get; set; } = new List<ContentApprovalLog>();
}

public enum ContentApprovalStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Published = 5
}

public sealed class ShopContentTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentId { get; set; }
    public required string LanguageCode { get; set; }
    public string? TranslatedText { get; set; }
    public bool IsAutoTranslated { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAtUtc { get; set; }

    public ShopContent? Content { get; set; }
}

public sealed class ContentApprovalLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentId { get; set; }
    public Guid? ApprovedByAdminId { get; set; }
    public ContentApprovalStatus OldStatus { get; set; }
    public ContentApprovalStatus NewStatus { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ShopContent? Content { get; set; }
    public User? ApprovedByAdmin { get; set; }
}
