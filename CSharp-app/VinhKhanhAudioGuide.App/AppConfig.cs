using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

public static class AppConfig
{
    // Demo user username - must match backend seed data
    public const string DefaultExternalRef = "demo";
    public const string DefaultPreferredLanguage = "vi";

    // Current user session (set after login)
    private static string? _currentUserId;
    private static bool _isLoggedIn = false;

    public static bool IsLoggedIn => _isLoggedIn;
    public static string? CurrentUserId => _currentUserId;

    public static void SetLoggedInUser(string userId)
    {
        _currentUserId = userId;
        _isLoggedIn = true;
        System.Diagnostics.Debug.WriteLine($"AppConfig: User logged in as {_currentUserId}");
    }

    public static void ClearUserSession()
    {
        _currentUserId = null;
        _isLoggedIn = false;
        FeatureGate.Clear();
        System.Diagnostics.Debug.WriteLine("AppConfig: User session cleared");
    }

    // Android emulator cannot call host machine through localhost.
    private const string AndroidEmulatorApiBaseUrl = "http://10.0.2.2:5140/api/v1/";
    private const string LocalApiBaseUrl = "http://localhost:5140/api/v1/";

    private const string AndroidEmulatorFrontendBaseUrl = "http://10.0.2.2:5173";
    private const string LocalFrontendBaseUrl = "http://localhost:5173";

    public static string ApiBaseUrl => DeviceInfo.Current.Platform == DevicePlatform.Android
        ? AndroidEmulatorApiBaseUrl
        : LocalApiBaseUrl;

    public static string FrontendBaseUrl => DeviceInfo.Current.Platform == DevicePlatform.Android
        ? AndroidEmulatorFrontendBaseUrl
        : LocalFrontendBaseUrl;

    // Backend serves static files at /media path (see Program.cs: app.UseStaticFiles with RequestPath = "/media")
    public static string MediaBaseUrl => ApiBaseUrl.Replace("/api/v1/", "/media/").TrimEnd('/');

    public static HttpClient CreateHttpClient()
    {
        return new HttpClient
        {
            BaseAddress = new Uri(ApiBaseUrl)
        };
    }

    public static async Task<string> ResolveDefaultUserIdAsync(HttpClient? client = null, CancellationToken cancellationToken = default)
    {
        // If already logged in via explicit login, return that user ID
        if (_isLoggedIn && !string.IsNullOrEmpty(_currentUserId))
        {
            return _currentUserId;
        }

        // Try to restore from SecureStorage first
        try
        {
            var storedId = await SecureStorage.GetAsync("user_id");
            if (!string.IsNullOrEmpty(storedId) && Guid.TryParse(storedId, out _))
            {
                _currentUserId = storedId;
                _isLoggedIn = true;
                System.Diagnostics.Debug.WriteLine($"AppConfig: Restored session for user {_currentUserId}");
                return storedId;
            }
        }
        catch { }

        // Load from SecureStorage for username/role
        string? storedUsername = null;
        string? storedPassword = null;
        try
        {
            storedUsername = await SecureStorage.GetAsync("username");
            storedPassword = await SecureStorage.GetAsync("password");
        }
        catch { }

        var ownsClient = client is null;
        client ??= CreateHttpClient();

        try
        {
            // Try stored credentials if available
            if (!string.IsNullOrEmpty(storedUsername))
            {
                var response = await client.PostAsJsonAsync("users/resolve", new
                {
                    Username = storedUsername,
                    PreferredLanguage = DefaultPreferredLanguage,
                    Password = string.IsNullOrEmpty(storedPassword) ? null : storedPassword
                }, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResult>(cancellationToken: cancellationToken);
                    if (result?.Success == true)
                    {
                        _currentUserId = result.Id.ToString();
                        _isLoggedIn = true;
                        return result.Id.ToString();
                    }
                }
            }

            // Fallback: resolve demo user
            var demoResponse = await client.PostAsJsonAsync("users/resolve", new
            {
                Username = DefaultExternalRef,
                PreferredLanguage = DefaultPreferredLanguage,
                Password = "1"
            }, cancellationToken);

            if (demoResponse.IsSuccessStatusCode)
            {
                var demoResult = await demoResponse.Content.ReadFromJsonAsync<LoginResult>(cancellationToken: cancellationToken);
                if (demoResult?.Success == true)
                {
                    _currentUserId = demoResult.Id.ToString();
                    _isLoggedIn = false; // Auto-resolved, not explicitly logged in
                    return demoResult.Id.ToString();
                }
            }

            // Last resort: anonymous ID
            return "00000000-0000-0000-0000-000000000001";
        }
        catch
        {
            return "00000000-0000-0000-0000-000000000001";
        }
        finally
        {
            if (ownsClient)
            {
                client.Dispose();
            }
        }
    }
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }
    public string? Plan { get; set; }
}
