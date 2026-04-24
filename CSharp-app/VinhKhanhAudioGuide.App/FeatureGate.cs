using System.Net.Http.Json;
using Microsoft.Maui.Storage;

namespace VinhKhanhAudioGuide.App;

    public static class FeatureGate
{
    // Feature codes matching backend
    public const string BasicPoi = "basic.poi";
    public const string PremiumAudio = "premium.segment.audio";
    public const string PremiumTour = "premium.segment.tour";

    private static bool? _isPremium;
    private static string? _cachedPlan;

    public static bool IsPremium => _isPremium ?? false;

    public static string CurrentPlan => _cachedPlan ?? "Basic";

    public static void SetPlan(string plan)
    {
        _cachedPlan = plan;
        _isPremium = plan.Contains("Premium", StringComparison.OrdinalIgnoreCase)
                     || plan.Contains("VIP", StringComparison.OrdinalIgnoreCase);
    }

    public static void Clear()
    {
        _isPremium = null;
        _cachedPlan = null;
    }

    public static async Task<bool> CheckAccessToSegmentAsync(string segmentCode, HttpClient? client = null)
    {
        if (_isPremium == true)
            return true;

        if (segmentCode.StartsWith("basic."))
            return true;

        return false;
    }

    public static async Task LoadFromSessionAsync()
    {
        try
        {
            var plan = await SecureStorage.GetAsync("plan");
            if (!string.IsNullOrEmpty(plan))
            {
                SetPlan(plan);
            }
        }
        catch { }
    }
}

public class PlanInfo
{
    public bool HasEnglishAudio { get; set; }
    public bool HasOfflineMode { get; set; }
    public bool HasAdvancedGps { get; set; }
    public bool HasPremiumTours { get; set; }
    public string PlanName { get; set; } = "Basic";
    public string PlanBadge { get; set; } = "[Basic]";

    public static PlanInfo FromPlan(string plan)
    {
        var isPremium = plan.Contains("Premium", StringComparison.OrdinalIgnoreCase)
                        || plan.Contains("VIP", StringComparison.OrdinalIgnoreCase);

        if (isPremium)
        {
            return new PlanInfo
            {
                HasEnglishAudio = true,
                HasOfflineMode = true,
                HasAdvancedGps = true,
                HasPremiumTours = true,
                PlanName = "Premium",
                PlanBadge = "[VIP] Premium"
            };
        }

        return new PlanInfo
        {
            HasEnglishAudio = false,
            HasOfflineMode = false,
            HasAdvancedGps = false,
            HasPremiumTours = false,
            PlanName = "Basic",
            PlanBadge = "[Basic]"
        };
    }
}
