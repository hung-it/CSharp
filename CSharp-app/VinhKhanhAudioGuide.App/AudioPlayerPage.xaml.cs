using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace VinhKhanhAudioGuide.App
{
    public partial class AudioPlayerPage : ContentPage
    {
        private const string LOADING_STATUS = "⏳ Đang tải...";
        private const string READY_STATUS = "✅ Sẵn sàng phát";
        private const string LOADING_AUDIO = "⏳ Đang tải audio...";
        private const string LOADING_PERCENT = "⏳ Đang tải... {0}%";
        private const string READY_PLAY = "✅ Sẵn sàng";
        private const string PLAYING_STATUS = "🎵 Đang phát...";
        private const string PAUSED_STATUS = "⏸️ Đã tạm dừng";
        private const string STOPPED_STATUS = "⏹️ Đã dừng";
        private const string COMPLETED_STATUS = "🎉 Đã phát xong!";
        private const string ERROR_LOAD = "❌ Lỗi tải audio";
        private const string ERROR_CONNECTION = "❌ Lỗi kết nối";
        private const string ERROR_MISSING_INFO = "❌ Lỗi: Thiếu thông tin";
        private const string PREMIUM_DIALOG = "💎 Premium";
        private const string ENGLISH_PREMIUM = "🔓 Audio tiếng Anh yêu cầu gói Premium";
        private const string PLAY_BUTTON = "▶️ Phát";
        private const string PAUSE_BUTTON = "⏸️ Tạm dừng";
        private const string SELECT_POI = "📍 Chọn POI";
        private const string POI_LIST = "Danh sách POI";
        private const string VIETNAMESE = "Tiếng Việt";
        private const string ENGLISH = "Tiếng Anh";
        private const string OK = "OK";

        private string currentPoiId = null;
        private string currentPoiName = null;
        private bool isPlaying = false;
        private bool isPaused = false;
        private double playbackSpeed = 1.0;

        public AudioPlayerPage()
        {
            InitializeComponent();
            InitializePlayer();
        }

        public AudioPlayerPage(string poiId, string poiName)
        {
            InitializeComponent();
            currentPoiId = poiId;
            currentPoiName = poiName;
            InitializePlayer();
        }

        private void InitializePlayer()
        {
            UpdateLanguageButtons();
            UpdateStatus(LOADING_STATUS);
            
            // Update POI info if available
            if (!string.IsNullOrEmpty(currentPoiName))
            {
                PoiNameLabel.Text = currentPoiName;
                AudioPoiLabel.Text = currentPoiName;
                StatusLabel.Text = READY_STATUS;
                UpdateStatus(READY_STATUS);
            }
            else
            {
                StatusLabel.Text = LOADING_STATUS;
            }

            // Check if English is available
            UpdatePremiumNote();
        }

        private void UpdateLanguageButtons()
        {
            if (AppConfig.CurrentLanguage == "vi")
            {
                VietnameseButton.BackgroundColor = Color.FromArgb("#FF69B4");
                VietnameseButton.TextColor = Colors.White;
                EnglishButton.BackgroundColor = Color.FromArgb("#E0E0E0");
                EnglishButton.TextColor = Color.FromArgb("#333333");
                AudioLanguageLabel.Text = VIETNAMESE;
            }
            else
            {
                VietnameseButton.BackgroundColor = Color.FromArgb("#E0E0E0");
                VietnameseButton.TextColor = Color.FromArgb("#333333");
                EnglishButton.BackgroundColor = Color.FromArgb("#FF69B4");
                EnglishButton.TextColor = Colors.White;
                AudioLanguageLabel.Text = ENGLISH;
            }
        }

        private void UpdatePremiumNote()
        {
            LanguageNoteLabel.IsVisible = !FeatureGate.HasEnglishAudio;
        }

        private void UpdateStatus(string status)
        {
            StatusLabel.Text = status;
            
            if (status.Contains("✅"))
            {
                StatusLabel.TextColor = Color.FromArgb("#4CAF50");
                ConnectionStatusLabel.Text = "✅";
                ConnectionTextLabel.Text = "Sẵn sàng phát";
            }
            else if (status.Contains("❌"))
            {
                StatusLabel.TextColor = Colors.Red;
                ConnectionStatusLabel.Text = "❌";
                ConnectionTextLabel.Text = "Lỗi";
            }
            else if (status.Contains("🎵"))
            {
                StatusLabel.TextColor = Color.FromArgb("#FF69B4");
                ConnectionStatusLabel.Text = "🎵";
                ConnectionTextLabel.Text = "Đang phát";
            }
            else if (status.Contains("⏸️"))
            {
                StatusLabel.TextColor = Color.FromArgb("#FFA500");
                ConnectionStatusLabel.Text = "⏸️";
                ConnectionTextLabel.Text = "Đã tạm dừng";
            }
            else
            {
                StatusLabel.TextColor = Color.FromArgb("#999999");
            }
        }

        private async void VietnameseButton_Clicked(object sender, EventArgs e)
        {
            AppConfig.CurrentLanguage = "vi";
            UpdateLanguageButtons();
            
            // Reload audio if playing
            if (isPlaying)
            {
                await StopAudioAsync();
            }
        }

        private async void EnglishButton_Clicked(object sender, EventArgs e)
        {
            if (!FeatureGate.HasEnglishAudio)
            {
                await DisplayAlert(PREMIUM_DIALOG, ENGLISH_PREMIUM, OK);
                return;
            }

            AppConfig.CurrentLanguage = "en";
            UpdateLanguageButtons();
            
            // Reload audio if playing
            if (isPlaying)
            {
                await StopAudioAsync();
            }
        }

        private async void PlayPauseButton_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentPoiId))
            {
                await DisplayAlert("❌", ERROR_MISSING_INFO, OK);
                return;
            }

            if (isPlaying)
            {
                await PauseAudioAsync();
            }
            else
            {
                await PlayAudioAsync();
            }
        }

        private async Task PlayAudioAsync()
        {
            try
            {
                UpdateStatus(LOADING_AUDIO);
                PlayPauseButton.Text = "⏳";
                PlayPauseButton.IsEnabled = false;

                // Simulate loading audio
                for (int i = 0; i <= 100; i += 20)
                {
                    UpdateStatus(string.Format(LOADING_PERCENT, i));
                    await Task.Delay(200);
                }

                // Start playing
                isPlaying = true;
                isPaused = false;
                PlayPauseButton.Text = PAUSE_BUTTON;
                PlayPauseButton.IsEnabled = true;
                UpdateStatus(PLAYING_STATUS);

                // Simulate playback progress
                await SimulatePlaybackAsync();
            }
            catch (Exception ex)
            {
                UpdateStatus(ERROR_LOAD);
                await DisplayAlert("❌", $"{ERROR_LOAD}: {ex.Message}", OK);
                PlayPauseButton.Text = PLAY_BUTTON;
                PlayPauseButton.IsEnabled = true;
            }
        }

        private async Task SimulatePlaybackAsync()
        {
            while (isPlaying && !isPaused)
            {
                await Task.Delay(1000);
                
                var currentValue = ProgressSlider.Value;
                if (currentValue >= 100)
                {
                    await OnPlaybackCompletedAsync();
                    break;
                }
                ProgressSlider.Value += (playbackSpeed * 2);
                
                var totalSeconds = 300; // 5 minutes
                var currentSeconds = (int)(totalSeconds * ProgressSlider.Value / 100);
                CurrentTimeLabel.Text = FormatTime(currentSeconds);
                TotalTimeLabel.Text = FormatTime(totalSeconds);
            }
        }

        private string FormatTime(int seconds)
        {
            var minutes = seconds / 60;
            var secs = seconds % 60;
            return $"{minutes}:{secs:D2}";
        }

        private async Task PauseAudioAsync()
        {
            isPaused = true;
            isPlaying = false;
            PlayPauseButton.Text = PLAY_BUTTON;
            UpdateStatus(PAUSED_STATUS);
            await Task.Delay(100);
        }

        private async Task StopAudioAsync()
        {
            isPlaying = false;
            isPaused = false;
            ProgressSlider.Value = 0;
            CurrentTimeLabel.Text = "0:00";
            PlayPauseButton.Text = PLAY_BUTTON;
            UpdateStatus(STOPPED_STATUS);
            await Task.Delay(100);
        }

        private async Task OnPlaybackCompletedAsync()
        {
            isPlaying = false;
            isPaused = false;
            PlayPauseButton.Text = PLAY_BUTTON;
            UpdateStatus(COMPLETED_STATUS);
            
            // Update stats
            AppConfig.ListenCount++;
            
            await Task.Delay(2000);
            UpdateStatus(READY_STATUS);
        }

        private async void PreviousButton_Clicked(object sender, EventArgs e)
        {
            ProgressSlider.Value = 0;
            if (isPlaying)
            {
                await StopAudioAsync();
            }
        }

        private async void NextButton_Clicked(object sender, EventArgs e)
        {
            if (isPlaying)
            {
                await StopAudioAsync();
            }
            await DisplayAlert("⏭️", "Đang chuyển sang audio tiếp theo...", OK);
        }

        private void SpeedButton_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button.Tag != null)
            {
                playbackSpeed = double.Parse(button.Tag.ToString());
            }

            // Update button styles
            Speed075Button.BackgroundColor = playbackSpeed == 0.75 ? Color.FromArgb("#FF69B4") : Color.FromArgb("#E0E0E0");
            Speed075Button.TextColor = playbackSpeed == 0.75 ? Colors.White : Color.FromArgb("#333333");
            Speed100Button.BackgroundColor = playbackSpeed == 1.0 ? Color.FromArgb("#FF69B4") : Color.FromArgb("#E0E0E0");
            Speed100Button.TextColor = playbackSpeed == 1.0 ? Colors.White : Color.FromArgb("#333333");
            Speed125Button.BackgroundColor = playbackSpeed == 1.25 ? Color.FromArgb("#FF69B4") : Color.FromArgb("#E0E0E0");
            Speed125Button.TextColor = playbackSpeed == 1.25 ? Colors.White : Color.FromArgb("#333333");
            Speed150Button.BackgroundColor = playbackSpeed == 1.5 ? Color.FromArgb("#FF69B4") : Color.FromArgb("#E0E0E0");
            Speed150Button.TextColor = playbackSpeed == 1.5 ? Colors.White : Color.FromArgb("#333333");
        }

        private async void SelectPoiButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PoiListPage());
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _ = StopAudioAsync();
        }
    }
}
