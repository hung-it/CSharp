using System;

namespace VinhKhanhAudioGuide.App
{
    /// <summary>
    /// Feature gates for controlling access to premium features
    /// </summary>
    public static class FeatureGate
    {
        // Feature flags
        public const string FEATURE_PREMIUM = "premium";
        public const string FEATURE_ENGLISH_AUDIO = "english_audio";
        public const string FEATURE_OFFLINE_MODE = "offline_mode";
        public const string FEATURE_ADVANCED_STATS = "advanced_stats";
        public const string FEATURE_PRIORITY_SUPPORT = "priority_support";
        public const string FEATURE_TOURS = "tours";

        /// <summary>
        /// Check if user has premium access
        /// </summary>
        public static bool HasPremiumAccess
        {
            get
            {
                return AppConfig.PlanType == "premium" || AppConfig.PlanType == "vip";
            }
        }

        /// <summary>
        /// Check if English audio is available (Premium only)
        /// </summary>
        public static bool HasEnglishAudio
        {
            get
            {
                return HasPremiumAccess || AppConfig.PlanType == "enterprise";
            }
        }

        /// <summary>
        /// Check if offline mode is available (Premium only)
        /// </summary>
        public static bool HasOfflineMode
        {
            get
            {
                return HasPremiumAccess || AppConfig.PlanType == "enterprise";
            }
        }

        /// <summary>
        /// Check if priority support is available (Premium only)
        /// </summary>
        public static bool HasPrioritySupport
        {
            get
            {
                return HasPremiumAccess || AppConfig.PlanType == "enterprise";
            }
        }

        /// <summary>
        /// Check if tours feature is available
        /// </summary>
        public static bool HasToursFeature
        {
            get
            {
                return true; // Available for all users
            }
        }

        /// <summary>
        /// Check if a specific feature is available
        /// </summary>
        public static bool IsFeatureEnabled(string feature)
        {
            switch (feature.ToLower())
            {
                case FEATURE_PREMIUM:
                    return HasPremiumAccess;
                case FEATURE_ENGLISH_AUDIO:
                    return HasEnglishAudio;
                case FEATURE_OFFLINE_MODE:
                    return HasOfflineMode;
                case FEATURE_PRIORITY_SUPPORT:
                    return HasPrioritySupport;
                case FEATURE_TOURS:
                    return HasToursFeature;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Get feature availability message
        /// </summary>
        public static string GetFeatureMessage(string feature)
        {
            switch (feature.ToLower())
            {
                case FEATURE_ENGLISH_AUDIO:
                    return HasEnglishAudio 
                        ? "✅ Audio tiếng Anh đã khả dụng" 
                        : "🔓 Audio tiếng Anh yêu cầu gói Premium";
                case FEATURE_OFFLINE_MODE:
                    return HasOfflineMode 
                        ? "✅ Chế độ Offline đã khả dụng" 
                        : "🔓 Chế độ Offline yêu cầu gói Premium";
                case FEATURE_PRIORITY_SUPPORT:
                    return HasPrioritySupport 
                        ? "✅ Hỗ trợ ưu tiên đã khả dụng" 
                        : "🔓 Hỗ trợ ưu tiên yêu cầu gói Premium";
                default:
                    return "Thông tin tính năng không có sẵn";
            }
        }

        /// <summary>
        /// Check if user can access a feature, showing upgrade dialog if not
        /// </summary>
        public static bool CanAccessFeature(string feature)
        {
            return IsFeatureEnabled(feature);
        }

        /// <summary>
        /// Get all available features for current plan
        /// </summary>
        public static string[] GetAvailableFeatures()
        {
            var features = new System.Collections.Generic.List<string>
            {
                FEATURE_PREMIUM
            };

            if (HasPremiumAccess)
            {
                features.Add(FEATURE_ENGLISH_AUDIO);
                features.Add(FEATURE_OFFLINE_MODE);
                features.Add(FEATURE_PRIORITY_SUPPORT);
            }

            features.Add(FEATURE_TOURS);

            return features.ToArray();
        }

        /// <summary>
        /// Get plan features description
        /// </summary>
        public static PlanInfo GetPlanInfo()
        {
            if (AppConfig.PlanType == "enterprise")
            {
                return new PlanInfo
                {
                    PlanName = "Enterprise",
                    IsPremium = true,
                    HasEnglishAudio = true,
                    HasOfflineMode = true,
                    HasPrioritySupport = true,
                    HasTours = true,
                    Features = new[] { "Audio đa ngôn ngữ", "Chế độ Offline", "Hỗ trợ ưu tiên", "Tours", "Analytics" }
                };
            }
            else if (AppConfig.PlanType == "premium" || AppConfig.PlanType == "vip")
            {
                return new PlanInfo
                {
                    PlanName = "Premium",
                    IsPremium = true,
                    HasEnglishAudio = true,
                    HasOfflineMode = true,
                    HasPrioritySupport = true,
                    HasTours = true,
                    Features = new[] { "Audio đa ngôn ngữ", "Chế độ Offline", "Hỗ trợ ưu tiên", "Tours" }
                };
            }
            else
            {
                return new PlanInfo
                {
                    PlanName = "Miễn phí",
                    IsPremium = false,
                    HasEnglishAudio = false,
                    HasOfflineMode = false,
                    HasPrioritySupport = false,
                    HasTours = true,
                    Features = new[] { "Audio tiếng Việt", "Bản đồ cơ bản", "Tours" }
                };
            }
        }
    }

    /// <summary>
    /// Plan information class
    /// </summary>
    public class PlanInfo
    {
        public string PlanName { get; set; }
        public bool IsPremium { get; set; }
        public bool HasEnglishAudio { get; set; }
        public bool HasOfflineMode { get; set; }
        public bool HasPrioritySupport { get; set; }
        public bool HasTours { get; set; }
        public string[] Features { get; set; }
    }
}
