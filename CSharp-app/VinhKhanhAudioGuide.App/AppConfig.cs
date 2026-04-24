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
                var storedPlan = await SecureStorage.GetAsync("plan");
                FeatureGate.SetPlan(storedPlan ?? "Basic");
                System.Diagnostics.Debug.WriteLine($"AppConfig: Restored session for user {_currentUserId}");
                return storedId;
            }
        }
        catch { }

        // Try stored credentials if available (username only, no password needed)
        string? storedUsername = null;
        string? storedUserId = null;
        try
        {
            storedUsername = await SecureStorage.GetAsync("username");
            storedUserId = await SecureStorage.GetAsync("user_id");
        }
        catch { }

        var ownsClient = client is null;
        client ??= CreateHttpClient();

        try
        {
            // Try stored credentials if available (no password for restore).
            // Send X-User-Id header so backend accepts session without password check.
            if (!string.IsNullOrEmpty(storedUsername))
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "users/resolve");
                request.Content = JsonContent.Create(new
                {
                    username = storedUsername,
                    preferredLanguage = DefaultPreferredLanguage
                });
                if (!string.IsNullOrEmpty(storedUserId))
                {
                    request.Headers.TryAddWithoutValidation("X-User-Id", storedUserId);
                }
                var response = await client.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResult>(cancellationToken: cancellationToken);
                    if (result?.Success == true)
                    {
                        _currentUserId = result.Id.ToString();
                        _isLoggedIn = true;
                        FeatureGate.SetPlan(result.Plan ?? "Basic");
                        return result.Id.ToString();
                    }
                }
            }

            // No stored credentials — attempt demo user resolution (creates account on demand).
            // Only fall back to the anonymous placeholder if the network call itself fails.
            try
            {
                var demoResponse = await client.PostAsJsonAsync("users/resolve", new
                {
                    Username = "demo",
                    PreferredLanguage = DefaultPreferredLanguage,
                    Password = "1"
                }, cancellationToken);

                if (demoResponse.IsSuccessStatusCode)
                {
                    var demoResult = await demoResponse.Content.ReadFromJsonAsync<LoginResult>(cancellationToken: cancellationToken);
                    if (demoResult?.Success == true)
                    {
                        _currentUserId = demoResult.Id.ToString();
                        _isLoggedIn = true;
                        FeatureGate.SetPlan(demoResult.Plan ?? "Basic");
                        return _currentUserId;
                    }
                }
            }
            catch { }

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
