using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

public static class AppConfig
{
    // Demo user username - must match backend seed data
    public const string DefaultExternalRef = "demo";
    public const string DefaultPreferredLanguage = "vi";

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
        var ownsClient = client is null;
        client ??= CreateHttpClient();

        try
        {
            // First try: resolve existing demo user with password
            var response = await client.PostAsJsonAsync("users/resolve", new
            {
                Username = DefaultExternalRef,
                PreferredLanguage = DefaultPreferredLanguage,
                Password = "1"
            }, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResult>(cancellationToken: cancellationToken);
                if (result?.Success == true)
                {
                    return result.Id.ToString();
                }
            }

            // Second try: create new anonymous user (no password required)
            var anonUsername = $"anon_{Guid.NewGuid():N}".Substring(0, 20);
            var createResponse = await client.PostAsJsonAsync("users/resolve", new
            {
                Username = anonUsername,
                PreferredLanguage = DefaultPreferredLanguage
            }, cancellationToken);

            if (createResponse.IsSuccessStatusCode)
            {
                var createResult = await createResponse.Content.ReadFromJsonAsync<LoginResult>(cancellationToken: cancellationToken);
                if (createResult?.Success == true)
                {
                    return createResult.Id.ToString();
                }
            }

            // Fallback: return a fixed anonymous ID for offline mode
            return "00000000-0000-0000-0000-000000000001";
        }
        catch
        {
            // Fallback for offline mode
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
