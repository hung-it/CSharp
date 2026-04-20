using VinhKhanhAudioGuide.Backend.Domain.Enums;

namespace VinhKhanhAudioGuide.Backend.Application.Authorization;

public static class UserAuthorizationExtensions
{
    /// <summary>
    /// Checks if user has admin or shop manager role.
    /// </summary>
    public static bool CanManagePoi(this UserRole role)
    {
        return role == UserRole.Admin || role == UserRole.ShopManager;
    }

    /// <summary>
    /// Checks if user can view all content (admin only).
    /// </summary>
    public static bool IsAdmin(this UserRole role)
    {
        return role == UserRole.Admin;
    }

    /// <summary>
    /// Checks if user is a shop manager.
    /// </summary>
    public static bool IsShopManager(this UserRole role)
    {
        return role == UserRole.ShopManager;
    }
}
