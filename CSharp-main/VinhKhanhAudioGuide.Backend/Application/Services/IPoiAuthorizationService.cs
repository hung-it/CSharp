using VinhKhanhAudioGuide.Backend.Domain.Enums;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Application.Services;

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
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken: cancellationToken);
        if (user is null)
        {
            return false;
        }

        return user.Role == UserRole.Admin || user.Role == UserRole.ShopManager;
    }

    public async Task<UserRole?> GetUserRoleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, cancellationToken: cancellationToken);
        return user?.Role;
    }
}
