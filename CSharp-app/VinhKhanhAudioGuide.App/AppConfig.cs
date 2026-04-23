using System;

namespace VinhKhanhAudioGuide.App
{
    /// <summary>
    /// Static configuration class for app settings and state
    /// </summary>
    public static class AppConfig
    {
        // App Info
        public const string AppVersion = "1.0.0";
        public const string AppName = "Phố Ăn Vĩnh Khách Audio Guide";
        public const string AppId = "com.phoananhvinhkhach.audioguide";

        // API Configuration
        public const string ApiBaseUrl = "https://api.phoananhvinhkhach.vn";
        public const string ApiVersion = "v1";
        public const int ApiTimeout = 30000;

        // Storage Keys
        private const string KEY_LANGUAGE = "app_language";
        private const string KEY_LOGGED_IN = "user_logged_in";
        private const string KEY_USER_NAME = "user_name";
        private const string KEY_USER_ID = "user_id";
        private const string KEY_AUTH_TOKEN = "auth_token";
        private const string KEY_PLAN_TYPE = "plan_type";
        private const string KEY_AUTO_PLAY = "auto_play_audio";
        private const string KEY_GPS_SENSITIVITY = "gps_sensitivity";
        private const string KEY_VOLUME = "audio_volume";
        private const string KEY_TTS_ENABLED = "tts_enabled";
        private const string KEY_OFFLINE_MODE = "offline_mode";
        private const string KEY_VISITED_COUNT = "visited_count";
        private const string KEY_LISTEN_COUNT = "listen_count";
        private const string KEY_LISTENED_MINUTES = "listened_minutes";
        private const string KEY_NEARBY_POIS = "nearby_pois";

        // User State
        private static bool _isLoggedIn = false;
        public static bool IsLoggedIn
        {
            get => _isLoggedIn;
            set => _isLoggedIn = value;
        }

        private static string _userName = null;
        public static string UserName
        {
            get => _userName;
            set => _userName = value;
        }

        private static string _userId = null;
        public static string UserId
        {
            get => _userId;
            set => _userId = value;
        }

        private static string _authToken = null;
        public static string AuthToken
        {
            get => _authToken;
            set => _authToken = value;
        }

        private static string _planType = "free";
        public static string PlanType
        {
            get => _planType;
            set => _planType = value;
        }

        // Settings
        private static string _currentLanguage = "vi";
        public static string CurrentLanguage
        {
            get => _currentLanguage;
            set => _currentLanguage = value;
        }

        private static bool _autoPlayAudio = true;
        public static bool AutoPlayAudio
        {
            get => _autoPlayAudio;
            set => _autoPlayAudio = value;
        }

        private static int _gpsSensitivity = 3;
        public static int GpsSensitivity
        {
            get => _gpsSensitivity;
            set => _gpsSensitivity = value;
        }

        private static int _volume = 80;
        public static int Volume
        {
            get => _volume;
            set => _volume = value;
        }

        private static bool _enableTts = false;
        public static bool EnableTts
        {
            get => _enableTts;
            set => _enableTts = value;
        }

        private static bool _offlineMode = false;
        public static bool OfflineMode
        {
            get => _offlineMode;
            set => _offlineMode = value;
        }

        // Stats
        private static int _visitedCount = 0;
        public static int VisitedCount
        {
            get => _visitedCount;
            set => _visitedCount = value;
        }

        private static int _listenCount = 0;
        public static int ListenCount
        {
            get => _listenCount;
            set => _listenCount = value;
        }

        private static int _listenedMinutes = 0;
        public static int ListenedMinutes
        {
            get => _listenedMinutes;
            set => _listenedMinutes = value;
        }

        // Nearby POIs
        private static System.Collections.Generic.List<PoiItem> _nearbyPois = new System.Collections.Generic.List<PoiItem>();
        public static System.Collections.Generic.List<PoiItem> NearbyPois
        {
            get => _nearbyPois;
            set => _nearbyPois = value ?? new System.Collections.Generic.List<PoiItem>();
        }

        // Initialize default values
        static AppConfig()
        {
            InitializeDefaults();
        }

        private static void InitializeDefaults()
        {
            // Set default values
            _currentLanguage = "vi";
            _autoPlayAudio = true;
            _gpsSensitivity = 3;
            _volume = 80;
            _enableTts = false;
            _offlineMode = false;
            _planType = "free";
        }

        /// <summary>
        /// Reset all settings to defaults
        /// </summary>
        public static void ResetToDefaults()
        {
            InitializeDefaults();
        }

        /// <summary>
        /// Clear user session
        /// </summary>
        public static void ClearSession()
        {
            _isLoggedIn = false;
            _userName = null;
            _userId = null;
            _authToken = null;
            _planType = "free";
        }

        /// <summary>
        /// Get full API URL for endpoint
        /// </summary>
        public static string GetApiUrl(string endpoint)
        {
            return $"{ApiBaseUrl}/{ApiVersion}/{endpoint.TrimStart('/')}";
        }

        /// <summary>
        /// Get localized string based on current language
        /// </summary>
        public static string GetString(string vietnamese, string english)
        {
            return _currentLanguage == "vi" ? vietnamese : english;
        }
    }

    /// <summary>
    /// POI Item class for nearby locations
    /// </summary>
    public class PoiItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string District { get; set; }
        public double Rating { get; set; }
        public double Distance { get; set; }
        public int Popularity { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string AudioUrlVi { get; set; }
        public string AudioUrlEn { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
