using VinhKhanhAudioGuide.Backend.Domain.Enums;
using VinhKhanhAudioGuide.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public sealed record PoiAccessContext(
    Guid UserId,
    UserRole Role,
    Guid? ManagedShopId)
{
    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsShopManager => Role == UserRole.ShopManager;
    public bool CanManagePoi => IsAdmin || (IsShopManager && ManagedShopId.HasValue);
}

public interface IPoiAuthorizationService
{
    /// <summary>
    /// Checks if a user can manage POIs (admin or shop manager).
    /// </summary>
    Task<bool> CanManagePoiAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user's role.
    /// </summary>
    Task<UserRole?> GetUserRoleAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve the caller access context for POI operations.
    /// </summary>
    Task<PoiAccessContext?> GetAccessContextAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether user can access a specific POI.
    /// </summary>
    Task<bool> CanAccessPoiAsync(Guid userId, Guid poiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether user can manage a POI in target shop.
    /// </summary>
    Task<bool> CanManagePoiInShopAsync(Guid userId, Guid? shopId, CancellationToken cancellationToken = default);
}

public sealed class PoiAuthorizationService : IPoiAuthorizationService
{
    private readonly AudioGuideDbContext _dbContext;

    public PoiAuthorizationService(AudioGuideDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CanManagePoiAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessContextAsync(userId, cancellationToken);
        return access?.CanManagePoi == true;
    }

    public async Task<UserRole?> GetUserRoleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken: cancellationToken);
        return user?.Role;
    }

    public async Task<PoiAccessContext?> GetAccessContextAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken: cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (user.Role == UserRole.Admin)
        {
            return new PoiAccessContext(userId, user.Role, null);
        }

        if (user.Role != UserRole.ShopManager)
        {
            return new PoiAccessContext(userId, user.Role, null);
        }

        var managedShopId = await _dbContext.ShopProfiles
            .Where(s => s.ManagerUserId == userId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new PoiAccessContext(userId, user.Role, managedShopId);
    }

    public async Task<bool> CanAccessPoiAsync(Guid userId, Guid poiId, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessContextAsync(userId, cancellationToken);
        if (access is null)
        {
            return false;
        }

        if (access.IsAdmin)
        {
            return true;
        }

        if (!access.IsShopManager || !access.ManagedShopId.HasValue)
        {
            return false;
        }

        var poi = await _dbContext.Pois
            .AsNoTracking()
            .Where(p => p.Id == poiId)
            .Select(p => new { p.Id, p.ShopId })
            .FirstOrDefaultAsync(cancellationToken);

        if (poi is null)
        {
            return false;
        }

        return poi.ShopId == access.ManagedShopId.Value;
    }

    public async Task<bool> CanManagePoiInShopAsync(Guid userId, Guid? shopId, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessContextAsync(userId, cancellationToken);
        if (access is null)
        {
            return false;
        }

        if (access.IsAdmin)
        {
            return true;
        }

        if (!access.IsShopManager || !access.ManagedShopId.HasValue || !shopId.HasValue)
        {
            return false;
        }

        return access.ManagedShopId.Value == shopId.Value;
    }
}
