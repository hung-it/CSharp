using System.Net.Http.Json;
using Microsoft.Maui.Storage;

namespace VinhKhanhAudioGuide.App;

public partial class SettingsPage : ContentPage
{
    private string _userId = string.Empty;
    private string _username = string.Empty;
    private string _plan = string.Empty;
    private bool _isLoggedIn = false;
    private bool _isInitializing = false;

    public SettingsPage()
    {
        InitializeComponent();
        AudioTypePicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshUserInfoAsync();
    }

    private async Task RefreshUserInfoAsync()
    {
        // Prevent multiple simultaneous refreshes
        if (_isInitializing) return;
        _isInitializing = true;

        try
        {
            await LoadUserInfoAsync();
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private async Task LoadUserInfoAsync()
    {
        try
        {
            // Always try to get saved session first
            var savedUserId = await SecureStorage.GetAsync("user_id");
            var savedUsername = await SecureStorage.GetAsync("username");
            var savedPlan = await SecureStorage.GetAsync("plan");
            var savedRole = await SecureStorage.GetAsync("role");

            System.Diagnostics.Debug.WriteLine($"[Settings] Checking saved session - UserId: {savedUserId}, Username: {savedUsername}");

            // If we have a valid saved session, use it
            if (!string.IsNullOrEmpty(savedUserId) && 
                !string.IsNullOrEmpty(savedUsername) && 
                savedUserId != "00000000-0000-0000-0000-000000000001" &&
                savedUserId.Length > 10)
            {
                _userId = savedUserId;
                _username = savedUsername;
                _plan = savedPlan ?? "Basic";
                _isLoggedIn = true;

                UpdateAccountUI();
                return;
            }

            // No valid saved session - try to resolve demo user
            var client = AppConfig.CreateHttpClient();
            var response = await client.PostAsJsonAsync("users/resolve", new
            {
                Username = "demo",
                PreferredLanguage = "vi",
                Password = "1"
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                if (result?.Success == true)
                {
                    _userId = result.Id.ToString();
                    _username = result.Username ?? "demo";
                    _plan = result.Plan ?? "Basic";
                    _isLoggedIn = true;

                    // Save session
                    await SecureStorage.SetAsync("user_id", _userId);
                    await SecureStorage.SetAsync("username", _username);
                    await SecureStorage.SetAsync("plan", _plan);
                    if (!string.IsNullOrEmpty(result.Role))
                    {
                        await SecureStorage.SetAsync("role", result.Role);
                    }

                    System.Diagnostics.Debug.WriteLine($"[Settings] Resolved demo user: {_username}, Plan: {_plan}");
                    UpdateAccountUI();
                    return;
                }
            }

            // Failed to resolve - reset to guest
            ResetToGuest();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Error loading user info: {ex.Message}");
            ResetToGuest();
        }
    }

    private void UpdateAccountUI()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UsernameLabel.Text = _username;
            PlanBadgeLabel.Text = _plan switch
            {
                "PremiumSegmented" => "👑 Premium (10 USD)",
                "Basic" => "📦 Basic (1 USD)",
                _ => $"📦 {_plan}"
            };
            LoginLogoutBtn.Text = "Đăng xuất";
            LoginStatusLabel.Text = $"Đã đăng nhập với gói {_plan}";
            LoginStatusLabel.IsVisible = false;

            // Show account info section with pink background
            var displayId = _userId.Length > 8 ? _userId.Substring(0, 8) + "..." : _userId;
            UserIdLabel.Text = displayId;
        });
    }

    private void ResetToGuest()
    {
        _userId = "00000000-0000-0000-0000-000000000001";
        _username = "Khách";
        _plan = "Chưa đăng nhập";
        _isLoggedIn = false;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            UsernameLabel.Text = "Khách";
            PlanBadgeLabel.Text = "Chưa đăng nhập";
            LoginLogoutBtn.Text = "Đăng nhập";
            LoginStatusLabel.Text = "Vui lòng đăng nhập để sử dụng đầy đủ tính năng";
            LoginStatusLabel.IsVisible = true;
            UserIdLabel.Text = "Guest";
        });
    }

    private async void OnLoginLogoutClicked(object? sender, EventArgs e)
    {
        if (_isLoggedIn)
        {
            // Logout
            var confirm = await DisplayAlertAsync("Đăng xuất",
                "Bạn có chắc muốn đăng xuất?",
                "Đăng xuất", "Hủy");

            if (confirm)
            {
                try
                {
                    await SecureStorage.SetAsync("user_id", "");
                    await SecureStorage.SetAsync("username", "");
                    await SecureStorage.SetAsync("plan", "");
                    await SecureStorage.SetAsync("role", "");
                }
                catch { }

                ResetToGuest();
            }
        }
        else
        {
            // Navigate to login page
            await Shell.Current.GoToAsync("//login");
        }
    }

    private void OnLanguageViClicked(object? sender, EventArgs e)
    {
        LangViBtn.BackgroundColor = Color.FromArgb("#EC4899");
        LangViBtn.TextColor = Colors.White;
        LangEnBtn.BackgroundColor = Color.FromArgb("#E5E7EB");
        LangEnBtn.TextColor = Color.FromArgb("#374151");
        
        // Save preference
        Preferences.Default.Set("language", "vi");
    }

    private void OnLanguageEnClicked(object? sender, EventArgs e)
    {
        LangEnBtn.BackgroundColor = Color.FromArgb("#EC4899");
        LangEnBtn.TextColor = Colors.White;
        LangViBtn.BackgroundColor = Color.FromArgb("#E5E7EB");
        LangViBtn.TextColor = Color.FromArgb("#374151");
        
        // Save preference
        Preferences.Default.Set("language", "en");
    }

    private void OnGpsSensitivityChanged(object? sender, ValueChangedEventArgs e)
    {
        GpsSensitivityLabel.Text = $"{(int)e.NewValue}m";
        Preferences.Default.Set("gpsSensitivity", (int)e.NewValue);
    }

    private void OnAudioTypeChanged(object? sender, EventArgs e)
    {
        Preferences.Default.Set("audioType", AudioTypePicker.SelectedIndex);
    }

    private void OnVolumeChanged(object? sender, ValueChangedEventArgs e)
    {
        VolumeLabel.Text = $"{(int)e.NewValue}%";
        Preferences.Default.Set("volume", (int)e.NewValue);
    }

    private async void OnOfflineModeToggled(object? sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            await DisplayAlertAsync("Chế độ Offline", 
                "Dữ liệu POI và audio sẽ được tải về khi có kết nối wifi. Bạn có thể sử dụng app mà không cần internet.", 
                "OK");
        }
        Preferences.Default.Set("offlineMode", e.Value);
    }

    private async void OnDownloadOfflineClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Tải dữ liệu offline", 
            "Đang tải dữ liệu POI và audio... Vui lòng đợi.", 
            "OK");
        
        // TODO: Implement actual offline data download
        await Task.Delay(2000);
        
        await DisplayAlertAsync("Thành công", 
            "Dữ liệu offline đã được tải về. Bạn có thể sử dụng app khi không có internet.", 
            "OK");
    }

    private async void OnClearCacheClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("Xóa cache", 
            "Bạn có chắc muốn xóa dữ liệu cache? Dữ liệu đã tải offline sẽ bị xóa.", 
            "Xóa", "Hủy");
        
        if (confirm)
        {
            // TODO: Implement actual cache clearing
            await DisplayAlertAsync("Thành công", "Đã xóa dữ liệu cache.", "OK");
        }
    }

    private async void OnOpenAdminWebClicked(object? sender, EventArgs e)
    {
        try
        {
            await Launcher.OpenAsync(new Uri(AppConfig.FrontendBaseUrl));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Lỗi", $"Không thể mở web: {ex.Message}", "OK");
        }
    }
}
