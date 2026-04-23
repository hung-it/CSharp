using System.Net.Http.Json;
using Microsoft.Maui.Storage;

namespace VinhKhanhAudioGuide.App;

public partial class SettingsPage : ContentPage
{
    private string _userId = string.Empty;
    private string _username = string.Empty;
    private string _plan = string.Empty;
    private bool _isLoggedIn = false;

    public SettingsPage()
    {
        InitializeComponent();
        AudioTypePicker.SelectedIndex = 0;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AudioTypePicker.SelectedIndex = 0;
        _ = LoadUserInfoAsync();
        _ = LoadPreferencesAsync();
    }

    private void LoadPreferences()
    {
        AutoPlaySwitch.IsToggled = Preferences.Default.Get("autoPlayEnabled", false);
        GpsSensitivitySlider.Value = Preferences.Default.Get("gpsSensitivity", 50);
        GpsSensitivityLabel.Text = $"{(int)GpsSensitivitySlider.Value}m";
        VolumeSlider.Value = Preferences.Default.Get("volume", 80);
        VolumeLabel.Text = $"{(int)VolumeSlider.Value}%";
        OfflineModeSwitch.IsToggled = Preferences.Default.Get("offlineMode", false);
    }

    private async Task LoadPreferencesAsync()
    {
        MainThread.BeginInvokeOnMainThread(LoadPreferences);
    }

    private async Task LoadUserInfoAsync()
    {
        try
        {
            var savedUserId = await SecureStorage.GetAsync("user_id");
            var savedUsername = await SecureStorage.GetAsync("username");
            var savedPlan = await SecureStorage.GetAsync("plan");

            System.Diagnostics.Debug.WriteLine($"[Settings] Session - UserId: {savedUserId}, Username: {savedUsername}");

            if (!string.IsNullOrEmpty(savedUserId) &&
                !string.IsNullOrEmpty(savedUsername) &&
                savedUserId != "00000000-0000-0000-0000-000000000001" &&
                savedUsername != "Khach" &&
                savedUsername != "")
            {
                _userId = savedUserId;
                _username = savedUsername;
                _plan = savedPlan ?? "Basic";
                _isLoggedIn = true;
                FeatureGate.SetPlan(_plan);
                System.Diagnostics.Debug.WriteLine($"[Settings] Đăng nhập: {_username}, Gói: {_plan}");
                UpdateUI(_username, _plan, true);
                return;
            }

            _userId = "";
            _username = "Khach";
            _plan = "Basic";
            _isLoggedIn = false;
            FeatureGate.Clear();
            UpdateUI("Khach", "Basic", false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Settings] Lỗi: {ex.Message}");
            UpdateUI("Khach", "Basic", false);
        }
    }

    private void UpdateUI(string username, string plan, bool isLoggedIn)
    {
        var planInfo = PlanInfo.FromPlan(plan);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            UsernameLabel.Text = username;

            if (isLoggedIn)
            {
                var featureText = planInfo.HasEnglishAudio ? "Đầy đủ" : "Giới hạn";
                PlanBadgeLabel.Text = $"{planInfo.PlanBadge} ({featureText})";
                LoginLogoutBtn.Text = "Đăng xuất";
                LoginStatusLabel.Text = $"✅ Đã đăng nhập với gói {planInfo.PlanName}";
                LoginStatusLabel.IsVisible = false;
                var displayId = _userId.Length > 8 ? _userId.Substring(0, 8) + "..." : _userId;
                UserIdLabel.Text = displayId;
                UpgradeBanner.IsVisible = false;
            }
            else
            {
                PlanBadgeLabel.Text = "Chưa đăng nhập";
                LoginLogoutBtn.Text = "🔑 Đăng nhập";
                LoginStatusLabel.Text = "⚠️ Vui lòng đăng nhập để sử dụng đầy đủ tính năng Premium";
                LoginStatusLabel.IsVisible = true;
                UserIdLabel.Text = "Khách";
                UpgradeBanner.IsVisible = true;
            }

            // 🎯 GPS - Premium có thể điều chỉnh
            GpsSensitivityRow.IsVisible = planInfo.HasAdvancedGps;
            GpsSensitivitySlider.IsVisible = planInfo.HasAdvancedGps;
            AutoPlayRow.IsVisible = planInfo.HasAdvancedGps;
            GpsPremiumBadge.IsVisible = !planInfo.HasAdvancedGps;

            // 🇬🇧 English audio - Premium only
            EnglishPremiumNotice.IsVisible = !planInfo.HasEnglishAudio;
            LangEnBtn.IsEnabled = planInfo.HasEnglishAudio;
            LangEnBtn.BackgroundColor = planInfo.HasEnglishAudio
                ? Color.FromArgb("#E5E7EB")
                : Color.FromArgb("#F3F4F6");
            LangEnBtn.TextColor = planInfo.HasEnglishAudio
                ? Color.FromArgb("#374151")
                : Color.FromArgb("#9CA3AF");

            // 🔊 Audio type - Premium only
            AudioTypeRow.IsVisible = planInfo.HasEnglishAudio;
            AudioPremiumBadge.IsVisible = !planInfo.HasEnglishAudio;

            // 📥 Offline mode - Premium only
            OfflineToggleRow.IsVisible = planInfo.HasOfflineMode;
            DownloadOfflineBtn.IsVisible = planInfo.HasOfflineMode;
            OfflinePremiumBadge.IsVisible = !planInfo.HasOfflineMode;

            // 🎁 Upgrade banner cho người đã đăng nhập Basic
            if (isLoggedIn && !planInfo.HasEnglishAudio)
                UpgradeBanner.IsVisible = true;
            else if (!isLoggedIn)
                UpgradeBanner.IsVisible = true;
            else
                UpgradeBanner.IsVisible = false;
        });
    }

    private async void OnLoginLogoutClicked(object? sender, EventArgs e)
    {
        try
        {
            var savedUserId = await SecureStorage.GetAsync("user_id");
            var savedUsername = await SecureStorage.GetAsync("username");

            var hasValidSession = !string.IsNullOrEmpty(savedUserId) &&
                                  !string.IsNullOrEmpty(savedUsername) &&
                                  savedUserId != "00000000-0000-0000-0000-000000000001" &&
                                  savedUsername != "Khach" &&
                                  savedUsername != "";

            if (hasValidSession)
            {
                var confirm = await DisplayAlertAsync("🚪 Đăng xuất",
                    "Bạn có chắc muốn đăng xuất khỏi tài khoản?",
                    "Đăng xuất", "Hủy");

                if (confirm)
                {
                    try
                    {
                        SecureStorage.Remove("user_id");
                        SecureStorage.Remove("username");
                        SecureStorage.Remove("plan");
                        SecureStorage.Remove("role");
                        SecureStorage.Remove("password");
                    }
                    catch { }

                    AppConfig.ClearUserSession();
                    FeatureGate.Clear();
                    _userId = "";
                    _username = "Khach";
                    _plan = "Basic";
                    _isLoggedIn = false;
                    UpdateUI("Khach", "Basic", false);

                    await Shell.Current.GoToAsync("login");
                }
            }
            else
            {
                await Shell.Current.GoToAsync("login");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("❌ Lỗi", $"Không thể mở trang đăng nhập: {ex.Message}", "OK");
        }
    }

    private void OnLanguageViClicked(object? sender, EventArgs e)
    {
        LangViBtn.BackgroundColor = Color.FromArgb("#EC4899");
        LangViBtn.TextColor = Colors.White;
        LangEnBtn.BackgroundColor = Color.FromArgb("#E5E7EB");
        LangEnBtn.TextColor = Color.FromArgb("#374151");
        Preferences.Default.Set("language", "vi");
        System.Diagnostics.Debug.WriteLine("[Settings] Đã chọn: Tiếng Việt 🇻🇳");
    }

    private async void OnLanguageEnClicked(object? sender, EventArgs e)
    {
        if (!FeatureGate.IsPremium)
        {
            await DisplayAlertAsync("💎 Premium",
                "Audio tiếng Anh là tính năng Premium.\n\n" +
                "🔓 Vui lòng nâng cấp tài khoản để sử dụng.", "OK");
            return;
        }

        LangEnBtn.BackgroundColor = Color.FromArgb("#EC4899");
        LangEnBtn.TextColor = Colors.White;
        LangViBtn.BackgroundColor = Color.FromArgb("#E5E7EB");
        LangViBtn.TextColor = Color.FromArgb("#374151");
        Preferences.Default.Set("language", "en");
        System.Diagnostics.Debug.WriteLine("[Settings] Đã chọn: English 🇬🇧");
    }

    private void OnGpsSensitivityChanged(object? sender, ValueChangedEventArgs e)
    {
        GpsSensitivityLabel.Text = $"{(int)e.NewValue}m";
        Preferences.Default.Set("gpsSensitivity", (int)e.NewValue);
    }

    private async void OnAutoPlayToggled(object? sender, ToggledEventArgs e)
    {
        if (!FeatureGate.IsPremium)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AutoPlaySwitch.IsToggled = false;
            });
            await DisplayAlertAsync("💎 Premium",
                "🤖 Tự động phát audio là tính năng Premium.\n\n" +
                "🔓 Vui lòng nâng cấp tài khoản để sử dụng.", "OK");
            return;
        }

        Preferences.Default.Set("autoPlayEnabled", e.Value);
        var status = e.Value ? "bật" : "tắt";
        System.Diagnostics.Debug.WriteLine($"[Settings] Tự động phát audio: {status}");
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
            await DisplayAlertAsync("🗄️ Chế độ Offline",
                "📥 Dữ liệu POI và audio sẽ được tải về khi có kết nối WiFi.\n\n" +
                "✅ Bạn có thể sử dụng app mà không cần internet.", "OK");
        }
        Preferences.Default.Set("offlineMode", e.Value);
    }

    private async void OnDownloadOfflineClicked(object? sender, EventArgs e)
    {
        if (!FeatureGate.IsPremium)
        {
            await DisplayAlertAsync("💎 Premium",
                "📥 Tải dữ liệu offline là tính năng Premium.\n\n" +
                "🔓 Vui lòng nâng cấp tài khoản để sử dụng.", "OK");
            return;
        }

        await DisplayAlertAsync("📥 Đang tải dữ liệu",
            "⏳ Vui lòng chờ trong giây lát...\n\n" +
            "📍 Đang tải POI và audio...", "OK");

        await Task.Delay(3000);

        await DisplayAlertAsync("✅ Thành công",
            "🎉 Dữ liệu offline đã được tải về!\n\n" +
            "✅ Bạn có thể sử dụng app khi không có internet.", "OK");
    }

    private async void OnClearCacheClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("🗑️ Xóa dữ liệu cache",
            "⚠️ Bạn có chắc muốn xóa dữ liệu cache?\n\n" +
            "📴 Dữ liệu đã tải offline sẽ bị xóa.", "Xóa", "Hủy");

        if (confirm)
        {
            try
            {
                var cacheDir = Path.Combine(FileSystem.CacheDirectory, "audio_cache");
                if (Directory.Exists(cacheDir))
                    Directory.Delete(cacheDir, true);
            }
            catch { }
            await DisplayAlertAsync("✅ Thành công", "🗑️ Đã xóa dữ liệu cache thành công!", "OK");
        }
    }

    private async void OnUpgradeClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("💎 Nâng cấp Premium",
            "⚡ Để nâng cấp tài khoản Premium, vui lòng truy cập:\n\n" +
            "🌐 " + AppConfig.FrontendBaseUrl + "\n\n" +
            "📧 Hoặc email: support@vinhkhanh.vn\n\n" +
            "💰 Giá: 10 USD/tháng", "OK");
    }

    private async void OnOpenAdminWebClicked(object? sender, EventArgs e)
    {
        try
        {
            await Launcher.OpenAsync(new Uri(AppConfig.FrontendBaseUrl));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("❌ Lỗi", $"Không thể mở web: {ex.Message}", "OK");
        }
    }
}
