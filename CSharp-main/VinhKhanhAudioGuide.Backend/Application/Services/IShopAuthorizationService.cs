using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Domain.Enums;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

public interface IShopAuthorizationService
{
    /// <summary>
    /// Check if user is shop owner
    /// </summary>
    Task<bool> IsShopOwnerAsync(Guid userId, Guid shopId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user can access shop (owner or admin)
    /// </summary>
    Task<bool> CanAccessShopAsync(Guid userId, Guid shopId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user can manage shop content
    /// </summary>
    Task<bool> CanManageShopContentAsync(Guid userId, Guid shopId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's shop ID if they are a shop manager
    /// </summary>
    Task<Guid?> GetUserShopIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class ShopAuthorizationService : IShopAuthorizationService
{
    private readonly AudioGuideDbContext _dbContext;

    public ShopAuthorizationService(AudioGuideDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsShopOwnerAsync(Guid userId, Guid shopId, CancellationToken cancellationToken = default)
    {
        var shop = await _dbContext.ShopProfiles.FindAsync(new object[] { shopId }, cancellationToken: cancellationToken);
        if (shop is null)
        {
            return false;
        }

        return shop.ManagerUserId == userId;
    }

    public async Task<bool> CanAccessShopAsync(Guid userId, Guid shopId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken: cancellationToken);
        if (user is null)
        {
            return false;
        }

        // Admin can access any shop
        if (user.Role == UserRole.Admin)
        {
            return true;
        }

        // ShopManager can access their own shop
        if (user.Role == UserRole.ShopManager)
        {
            return await IsShopOwnerAsync(userId, shopId, cancellationToken);
        }

        return false;
    }

    public async Task<bool> CanManageShopContentAsync(Guid userId, Guid shopId, CancellationToken cancellationToken = default)
    {
        return await CanAccessShopAsync(userId, shopId, cancellationToken);
    }

    public async Task<Guid?> GetUserShopIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var shop = await _dbContext.ShopProfiles
            .Where(s => s.ManagerUserId == userId)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return shop == Guid.Empty ? null : shop;
    }
}
