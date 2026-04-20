using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IShopManagementService
{
    // Shop Profile
    Task<ShopProfile> CreateShopAsync(Guid managerUserId, string name, string? description, string? address, string? mapLink, string? openingHours, string? avatarUrl, string? coverImageUrl, CancellationToken cancellationToken = default);
    Task<ShopProfile> UpdateShopAsync(Guid shopId, string? name, string? description, string? address, string? mapLink, string? openingHours, string? avatarUrl, string? coverImageUrl, CancellationToken cancellationToken = default);
    Task<ShopProfile?> GetShopByIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<ShopProfile?> GetShopByManagerAsync(Guid managerUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopProfile>> GetShopsByStatusAsync(ShopVerificationStatus status, CancellationToken cancellationToken = default);
    
    // Shop Content
    Task<ShopContent> CreateContentAsync(Guid shopId, Guid poiId, string? textScript, CancellationToken cancellationToken = default);
    Task<ShopContent> UpdateContentAsync(Guid contentId, string? textScript, CancellationToken cancellationToken = default);
    Task<ShopContent?> GetContentByIdAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopContent>> GetContentByShopAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopContent>> GetContentByPoiAsync(Guid poiId, CancellationToken cancellationToken = default);
    
    // Content Submission
    Task SubmitContentForApprovalAsync(Guid contentId, CancellationToken cancellationToken = default);
    Task ApproveContentAsync(Guid contentId, Guid adminUserId, string? notes, CancellationToken cancellationToken = default);
    Task RejectContentAsync(Guid contentId, Guid adminUserId, string rejectionReason, CancellationToken cancellationToken = default);
    
    // Translation
    Task<ShopContentTranslation> UpsertTranslationAsync(Guid contentId, string languageCode, string translatedText, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShopContentTranslation>> GetTranslationsByContentAsync(Guid contentId, CancellationToken cancellationToken = default);
}

public sealed class ShopManagementService : IShopManagementService
{
    private readonly AudioGuideDbContext _dbContext;

    public ShopManagementService(AudioGuideDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ShopProfile> CreateShopAsync(Guid managerUserId, string name, string? description, string? address, string? mapLink, string? openingHours, string? avatarUrl, string? coverImageUrl, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ShopProfiles
            .FirstOrDefaultAsync(s => s.ManagerUserId == managerUserId, cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException("User already has a shop profile.");
        }

        var shop = new ShopProfile
        {
            ManagerUserId = managerUserId,
            Name = name,
            Description = description,
            Address = address,
            MapLink = mapLink,
            OpeningHours = openingHours,
            AvatarUrl = avatarUrl,
            CoverImageUrl = coverImageUrl,
            VerificationStatus = ShopVerificationStatus.Pending
        };

        _dbContext.ShopProfiles.Add(shop);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return shop;
    }

    public async Task<ShopProfile> UpdateShopAsync(Guid shopId, string? name, string? description, string? address, string? mapLink, string? openingHours, string? avatarUrl, string? coverImageUrl, CancellationToken cancellationToken = default)
    {
        var shop = await _dbContext.ShopProfiles.FindAsync(new object[] { shopId }, cancellationToken: cancellationToken);
        if (shop is null)
        {
            throw new KeyNotFoundException("Shop not found.");
        }

        if (name is not null)
            shop.Name = name;
        if (description is not null)
            shop.Description = description;
        if (address is not null)
            shop.Address = address;
        if (mapLink is not null)
            shop.MapLink = mapLink;
        if (openingHours is not null)
            shop.OpeningHours = openingHours;
        if (avatarUrl is not null)
            shop.AvatarUrl = avatarUrl;
        if (coverImageUrl is not null)
            shop.CoverImageUrl = coverImageUrl;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return shop;
    }

    public async Task<ShopProfile?> GetShopByIdAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopProfiles.FindAsync(new object[] { shopId }, cancellationToken: cancellationToken);
    }

    public async Task<ShopProfile?> GetShopByManagerAsync(Guid managerUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopProfiles
            .FirstOrDefaultAsync(s => s.ManagerUserId == managerUserId, cancellationToken);
    }

    public async Task<IEnumerable<ShopProfile>> GetShopsByStatusAsync(ShopVerificationStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopProfiles
            .Where(s => s.VerificationStatus == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShopContent> CreateContentAsync(Guid shopId, Guid poiId, string? textScript, CancellationToken cancellationToken = default)
    {
        var content = new ShopContent
        {
            ShopId = shopId,
            PoiId = poiId,
            TextScript = textScript,
            ApprovalStatus = ContentApprovalStatus.Draft
        };

        _dbContext.ShopContents.Add(content);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return content;
    }

    public async Task<ShopContent> UpdateContentAsync(Guid contentId, string? textScript, CancellationToken cancellationToken = default)
    {
        var content = await _dbContext.ShopContents.FindAsync(new object[] { contentId }, cancellationToken: cancellationToken);
        if (content is null)
        {
            throw new KeyNotFoundException("Content not found.");
        }

        if (textScript is not null)
            content.TextScript = textScript;
        content.LastModifiedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return content;
    }

    public async Task<ShopContent?> GetContentByIdAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopContents
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.Id == contentId, cancellationToken);
    }

    public async Task<IEnumerable<ShopContent>> GetContentByShopAsync(Guid shopId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopContents
            .Where(c => c.ShopId == shopId)
            .Include(c => c.Translations)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShopContent>> GetContentByPoiAsync(Guid poiId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopContents
            .Where(c => c.PoiId == poiId)
            .Include(c => c.Translations)
            .ToListAsync(cancellationToken);
    }

    public async Task SubmitContentForApprovalAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        var content = await _dbContext.ShopContents.FindAsync(new object[] { contentId }, cancellationToken: cancellationToken);
        if (content is null)
        {
            throw new KeyNotFoundException("Content not found.");
        }

        if (content.ApprovalStatus != ContentApprovalStatus.Draft)
        {
            throw new InvalidOperationException("Only draft content can be submitted.");
        }

        content.ApprovalStatus = ContentApprovalStatus.PendingApproval;
        content.SubmittedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveContentAsync(Guid contentId, Guid adminUserId, string? notes, CancellationToken cancellationToken = default)
    {
        var content = await _dbContext.ShopContents.FindAsync(new object[] { contentId }, cancellationToken: cancellationToken);
        if (content is null)
        {
            throw new KeyNotFoundException("Content not found.");
        }

        var oldStatus = content.ApprovalStatus;
        content.ApprovalStatus = ContentApprovalStatus.Approved;
        content.ApprovedAtUtc = DateTime.UtcNow;

        var log = new ContentApprovalLog
        {
            ContentId = contentId,
            ApprovedByAdminId = adminUserId,
            OldStatus = oldStatus,
            NewStatus = ContentApprovalStatus.Approved,
            AdminNotes = notes
        };

        _dbContext.ContentApprovalLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectContentAsync(Guid contentId, Guid adminUserId, string rejectionReason, CancellationToken cancellationToken = default)
    {
        var content = await _dbContext.ShopContents.FindAsync(new object[] { contentId }, cancellationToken: cancellationToken);
        if (content is null)
        {
            throw new KeyNotFoundException("Content not found.");
        }

        var oldStatus = content.ApprovalStatus;
        content.ApprovalStatus = ContentApprovalStatus.Rejected;
        content.RejectionReason = rejectionReason;

        var log = new ContentApprovalLog
        {
            ContentId = contentId,
            ApprovedByAdminId = adminUserId,
            OldStatus = oldStatus,
            NewStatus = ContentApprovalStatus.Rejected,
            AdminNotes = rejectionReason
        };

        _dbContext.ContentApprovalLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShopContentTranslation> UpsertTranslationAsync(Guid contentId, string languageCode, string translatedText, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ShopContentTranslations
            .FirstOrDefaultAsync(t => t.ContentId == contentId && t.LanguageCode == languageCode, cancellationToken);

        if (existing is not null)
        {
            existing.TranslatedText = translatedText;
            existing.ModifiedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var translation = new ShopContentTranslation
        {
            ContentId = contentId,
            LanguageCode = languageCode,
            TranslatedText = translatedText,
            IsAutoTranslated = false
        };

        _dbContext.ShopContentTranslations.Add(translation);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return translation;
    }

    public async Task<IEnumerable<ShopContentTranslation>> GetTranslationsByContentAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShopContentTranslations
            .Where(t => t.ContentId == contentId)
            .ToListAsync(cancellationToken);
    }
}
