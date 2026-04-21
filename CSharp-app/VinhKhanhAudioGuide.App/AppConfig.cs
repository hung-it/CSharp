using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

public static class AppConfig
{
    public const string DefaultExternalRef = "USER_DEMO";
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

    public static string MediaBaseUrl => ApiBaseUrl.Replace("/api/v1/", "/").TrimEnd('/');

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
            var response = await client.PostAsJsonAsync("users/resolve", new
            {
                ExternalRef = DefaultExternalRef,
                PreferredLanguage = DefaultPreferredLanguage
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var user = await response.Content.ReadFromJsonAsync<ResolvedUserResponse>(cancellationToken: cancellationToken);
            return user?.Id.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
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

public sealed class ResolvedUserResponse
{
    public Guid Id { get; set; }
    public string ExternalRef { get; set; } = string.Empty;
    public string? Role { get; set; }
}
