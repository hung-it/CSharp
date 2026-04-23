using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VinhKhanhAudioGuide.App
{
    public partial class SettingsPage : ContentPage
    {
        private const string PREMIUM_DIALOG = "💎 Premium";
        private const string LOGIN_REQUIRED_DIALOG = "⚠️ Vui lòng đăng nhập để sử dụng tính năng này";
        private const string UPGRADE_REQUIRED_DIALOG = "🔓 Vui lòng nâng cấp lên gói Premium để sử dụng tính năng này";
        private const string ENGLISH_AUDIO_PREMIUM = "🔓 Tính năng audio tiếng Anh yêu cầu gói Premium";
        private const string OFFLINE_PREMIUM = "🔓 Chế độ Offline yêu cầu gói Premium";
        private const string OFFLINE_DOWNLOADING = "📥 Đang tải dữ liệu offline...";
        private const string OFFLINE_COMPLETE = "✅ Đã tải xong dữ liệu offline";
        private const string OFFLINE_ERROR = "❌ Lỗi tải dữ liệu offline";
        private const string LOGOUT_CONFIRM = "Bạn có chắc muốn đăng xuất?";
        private const string YES = "Có";
        private const string NO = "Không";
        private const string LOGOUT_SUCCESS = "✅ Đã đăng xuất";
        private const string GPS_HIGH = "Cao";
        private const string GPS_MEDIUM = "Trung bình";
        private const string GPS_LOW = "Thấp";

        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load version
            VersionLabel.Text = AppConfig.AppVersion;
            
            // Load user info
            if (AppConfig.IsLoggedIn)
            {
                UserNameLabel.Text = AppConfig.UserName ?? "Người dùng";
                LoginLogoutButton.Text = "🔓 Đăng xuất";
                UserIdLabel.Text = AppConfig.UserId ?? "---";
            }
            else
            {
                UserNameLabel.Text = "Khách";
                LoginLogoutButton.Text = "🔑 Đăng nhập";
                UserIdLabel.Text = "---";
            }

            // Load language setting
            UpdateLanguageButtons();

            // Load GPS sensitivity
            GpsSensitivitySlider.Value = AppConfig.GpsSensitivity;

            // Load audio settings
            AutoPlaySwitch.IsToggled = AppConfig.AutoPlayAudio;
            VolumeSlider.Value = AppConfig.Volume;
            TtsSwitch.IsToggled = AppConfig.EnableTts;

            // Load offline mode
            OfflineModeSwitch.IsToggled = AppConfig.OfflineMode;

            // Update premium button state
            UpdatePremiumButton();
        }

        private void UpdateLanguageButtons()
        {
            if (AppConfig.CurrentLanguage == "vi")
            {
                VietnameseButton.BackgroundColor = Color.FromArgb("#FF69B4");
                VietnameseButton.TextColor = Colors.White;
                EnglishButton.BackgroundColor = Color.FromArgb("#E0E0E0");
                EnglishButton.TextColor = Color.FromArgb("#333333");
            }
            else
            {
                VietnameseButton.BackgroundColor = Color.FromArgb("#E0E0E0");
                VietnameseButton.TextColor = Color.FromArgb("#333333");
                EnglishButton.BackgroundColor = Color.FromArgb("#FF69B4");
                EnglishButton.TextColor = Colors.White;
            }
        }

        private void UpdatePremiumButton()
        {
            if (FeatureGate.HasPremiumAccess)
            {
                DownloadOfflineButton.IsEnabled = true;
                DownloadOfflineButton.Text = "📥 Tải dữ liệu offline";
            }
            else
            {
                DownloadOfflineButton.IsEnabled = true;
                DownloadOfflineButton.Text = "📥 Tải dữ liệu offline";
            }
        }

        private async void LoginLogoutButton_Clicked(object sender, EventArgs e)
        {
            if (AppConfig.IsLoggedIn)
            {
                var result = await DisplayAlert("🔓 Đăng xuất", LOGOUT_CONFIRM, YES, NO);
                if (result)
                {
                    AppConfig.IsLoggedIn = false;
                    AppConfig.UserName = null;
                    AppConfig.UserId = null;
                    AppConfig.AuthToken = null;
                    UserNameLabel.Text = "Khách";
                    LoginLogoutButton.Text = "🔑 Đăng nhập";
                    UserIdLabel.Text = "---";
                    await DisplayAlert("✅", LOGOUT_SUCCESS, "OK");
                }
            }
            else
            {
                await Navigation.PushAsync(new LoginPage());
            }
        }

        private async void UpgradeButton_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert(PREMIUM_DIALOG, "✨ Bạn có thể nâng cấp lên Premium để sử dụng:\n\n💎 Audio tiếng Anh\n🗄️ Chế độ Offline\n📊 Thống kê nâng cao\n⚡ Ưu tiên hỗ trợ", "Nâng cấp ngay");
        }

        private void VietnameseButton_Clicked(object sender, EventArgs e)
        {
            AppConfig.CurrentLanguage = "vi";
            UpdateLanguageButtons();
        }

        private void EnglishButton_Clicked(object sender, EventArgs e)
        {
            if (!FeatureGate.HasEnglishAudio)
            {
                DisplayAlert(PREMIUM_DIALOG, ENGLISH_AUDIO_PREMIUM, "OK");
                return;
            }
            AppConfig.CurrentLanguage = "en";
            UpdateLanguageButtons();
        }

        private void AutoPlaySwitch_Toggled(object sender, EventArgs e)
        {
            AppConfig.AutoPlayAudio = AutoPlaySwitch.IsToggled;
        }

        private void GpsSensitivitySlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            int sensitivity = (int)Math.Round(e.NewValue);
            AppConfig.GpsSensitivity = sensitivity;
            
            switch (sensitivity)
            {
                case 1:
                    GpsSensitivityLabel.Text = GPS_LOW;
                    break;
                case 2:
                    GpsSensitivityLabel.Text = "Khá thấp";
                    break;
                case 3:
                    GpsSensitivityLabel.Text = GPS_MEDIUM;
                    break;
                case 4:
                    GpsSensitivityLabel.Text = "Khá cao";
                    break;
                case 5:
                    GpsSensitivityLabel.Text = GPS_HIGH;
                    break;
            }
        }

        private void TtsSwitch_Toggled(object sender, EventArgs e)
        {
            AppConfig.EnableTts = TtsSwitch.IsToggled;
        }

        private async void OfflineModeSwitch_Toggled(object sender, EventArgs e)
        {
            if (OfflineModeSwitch.IsToggled && !FeatureGate.HasOfflineMode)
            {
                await DisplayAlert(PREMIUM_DIALOG, OFFLINE_PREMIUM, "OK");
                OfflineModeSwitch.IsToggled = false;
                return;
            }
            AppConfig.OfflineMode = OfflineModeSwitch.IsToggled;
        }

        private async void DownloadOfflineButton_Clicked(object sender, EventArgs e)
        {
            if (!FeatureGate.HasOfflineMode)
            {
                await DisplayAlert(PREMIUM_DIALOG, OFFLINE_PREMIUM, "Nâng cấp");
                return;
            }

            DownloadOfflineButton.Text = OFFLINE_DOWNLOADING;
            DownloadOfflineButton.IsEnabled = false;

            try
            {
                // Simulate download
                await Task.Delay(2000);
                
                DownloadOfflineButton.Text = OFFLINE_COMPLETE;
                await DisplayAlert("✅", OFFLINE_COMPLETE, "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("❌", $"{OFFLINE_ERROR}: {ex.Message}", "OK");
            }
            finally
            {
                DownloadOfflineButton.Text = "📥 Tải dữ liệu offline";
                DownloadOfflineButton.IsEnabled = true;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadSettings();
        }
    }
}
