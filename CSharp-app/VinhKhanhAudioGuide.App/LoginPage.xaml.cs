using System.Net.Http.Json;
using Microsoft.Maui.Storage;

namespace VinhKhanhAudioGuide.App;

public partial class LoginPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public LoginPage()
    {
        InitializeComponent();
        _httpClient = AppConfig.CreateHttpClient();
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var username = UsernameEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(username))
        {
            ShowError("⚠️ Vui lòng nhập tên đăng nhập");
            return;
        }

        await PerformLoginAsync(username, password);
    }

    private async void OnDemoBasicClicked(object? sender, EventArgs e)
    {
        UsernameEntry.Text = "demo";
        PasswordEntry.Text = "1";
        await PerformLoginAsync("demo", "1");
    }

    private async void OnDemoPremiumClicked(object? sender, EventArgs e)
    {
        UsernameEntry.Text = "premium";
        PasswordEntry.Text = "1";
        await PerformLoginAsync("premium", "1");
    }

    private async void OnRegisterTapped(object? sender, EventArgs e)
    {
        UsernameEntry.Text = "";
        PasswordEntry.Text = "";
        ErrorLabel.Text = "📧 Liên hệ quản trị viên để tạo tài khoản mới";
        ErrorLabel.IsVisible = true;
    }

    private async void OnSkipClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//home");
    }

    private async Task PerformLoginAsync(string username, string password)
    {
        try
        {
            SetLoading(true);
            ErrorLabel.IsVisible = false;

            var response = await _httpClient.PostAsJsonAsync("users/resolve", new
            {
                Username = username,
                PreferredLanguage = AppConfig.DefaultPreferredLanguage,
                Password = password
            });

            if (!response.IsSuccessStatusCode)
            {
                ShowError("❌ Không thể kết nối server. Vui lòng thử lại.");
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResult>();

            if (result?.Success == true)
            {
                await SaveUserSessionAsync(result);
                await Shell.Current.GoToAsync("//accountTab");
            }
            else
            {
                ShowError(result?.Message ?? "❌ Đăng nhập thất bại");
            }
        }
        catch (HttpRequestException)
        {
            ShowError("❌ Không thể kết nối server. Đang hoạt động offline.");
            await NavigateToMainPage();
        }
        catch (Exception ex)
        {
            ShowError($"❌ Lỗi: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task SaveUserSessionAsync(LoginResult result)
    {
        try
        {
            await SecureStorage.SetAsync("user_id", result.Id.ToString());
            await SecureStorage.SetAsync("username", result.Username ?? "");
            await SecureStorage.SetAsync("role", result.Role ?? "EndUser");
            await SecureStorage.SetAsync("plan", result.Plan ?? "Basic");
            await SecureStorage.SetAsync("password", PasswordEntry.Text ?? "");

            AppConfig.SetLoggedInUser(result.Id.ToString());
            FeatureGate.SetPlan(result.Plan ?? "Basic");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save session: {ex.Message}");
        }
    }

    private async Task NavigateToMainPage()
    {
        await Shell.Current.GoToAsync("home");
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetLoading(bool isLoading)
    {
        LoadingIndicator.IsRunning = isLoading;
        LoadingIndicator.IsVisible = isLoading;
        LoginBtn.IsEnabled = !isLoading;
        UsernameEntry.IsEnabled = !isLoading;
        PasswordEntry.IsEnabled = !isLoading;
    }
}
