using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Newtonsoft.Json.Linq;

namespace VinhKhanhAudioGuide.App
{
    public partial class MainPage : ContentPage
    {
        private const string CONNECTED_MESSAGE = "✅ Đã kết nối server";
        private const string OFFLINE_MESSAGE = "⚠️ Chế độ offline";
        private const string CONNECTING_MESSAGE = "⏳ Đang kết nối...";
        private const string QUICK_LOGIN_DEMO = "🔑 Đang đăng nhập nhanh (demo)...";
        private const string UPDATING_MESSAGE = "⏳ đang cập nhật...";
        private const string LOADING_ERROR = "❌ Lỗi tải dữ liệu";
        private const string NO_NEARBY = "😴 Không có điểm gần đây";
        private const string NO_LOGIN_REQUIRED = "⚠️ Vui lòng đăng nhập để sử dụng tính năng này";
        private const string LANGUAGE_DIALOG = "🌐 Đổi ngôn ngữ";
        private const string CHANGE_LANGUAGE = "Bạn có muốn chuyển sang tiếng Anh không?";
        private const string VIETNAMESE = "Tiếng Việt";
        private const string ENGLISH = "Tiếng Anh";
        private const string CANCEL = "Hủy";

        public MainPage()
        {
            InitializeComponent();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            // Show connecting status
            ConnectionStatusLabel.Text = CONNECTING_MESSAGE;
            
            // Try to login quickly for demo
            await TryQuickLoginAsync();
            
            // Check connection
            await CheckConnectionAsync();
            
            // Load stats
            await LoadStatsAsync();
            
            // Load nearby POIs
            await LoadNearbyPoisAsync();
            
            // Load popular POIs
            await LoadPopularPoisAsync();
        }

        private async Task TryQuickLoginAsync()
        {
            try
            {
                if (!AppConfig.IsLoggedIn)
                {
                    // Quick demo login
                    AppConfig.IsLoggedIn = true;
                    AppConfig.UserName = "Khách Demo";
                    AppConfig.UserId = Guid.NewGuid().ToString().Substring(0, 8);
                    AppConfig.AuthToken = "demo_token";
                }
            }
            catch
            {
                // Ignore login errors
            }
        }

        private async Task CheckConnectionAsync()
        {
            try
            {
                // Simulate connection check
                await Task.Delay(500);
                
                if (AppConfig.OfflineMode)
                {
                    ConnectionStatusLabel.Text = OFFLINE_MESSAGE;
                    ConnectionStatusLabel.TextColor = Colors.Orange;
                }
                else
                {
                    ConnectionStatusLabel.Text = CONNECTED_MESSAGE;
                    ConnectionStatusLabel.TextColor = Colors.LightGreen;
                }
            }
            catch
            {
                ConnectionStatusLabel.Text = OFFLINE_MESSAGE;
                ConnectionStatusLabel.TextColor = Colors.Orange;
            }
        }

        private async Task LoadStatsAsync()
        {
            try
            {
                // Load user stats
                VisitedCountLabel.Text = AppConfig.VisitedCount.ToString();
                ListenCountLabel.Text = AppConfig.ListenCount.ToString();
                MinutesLabel.Text = AppConfig.ListenedMinutes.ToString();
                
                // Simulate loading
                await Task.Delay(300);
            }
            catch
            {
                // Use default values
            }
        }

        private async Task LoadNearbyPoisAsync()
        {
            NearbyActivityIndicator.IsRunning = true;
            
            try
            {
                // Simulate loading nearby POIs
                await Task.Delay(1000);
                
                // Check if there are any nearby POIs
                if (AppConfig.NearbyPois != null && AppConfig.NearbyPois.Count > 0)
                {
                    NoNearbyLabel.IsVisible = false;
                    
                    foreach (var poi in AppConfig.NearbyPois.Take(5))
                    {
                        var poiCard = CreatePoiCard(poi);
                        NearbyPoiContainer.Children.Add(poiCard);
                    }
                }
                else
                {
                    NoNearbyLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                NoNearbyLabel.IsVisible = true;
            }
            finally
            {
                NearbyActivityIndicator.IsRunning = false;
            }
        }

        private async Task LoadPopularPoisAsync()
        {
            try
            {
                // Simulate loading popular POIs
                await Task.Delay(800);
                
                // Sample popular POIs
                var popularPois = new List<PoiItem>
                {
                    new PoiItem { Id = "1", Name = "Quán ăn Vĩnh Hạnh", Category = "🏠 Nhà hàng", Rating = 4.8 },
                    new PoiItem { Id = "2", Name = "Bánh xèo Tư Hòa", Category = "🍜 Ẩm thực", Rating = 4.6 },
                    new PoiItem { Id = "3", Name = "Chùa Vĩnh Nghiêm", Category = "🏛️ Di tích", Rating = 4.9 },
                    new PoiItem { Id = "4", Name = "Cà phê Sữa Đá", Category = "☕ Quán cà phê", Rating = 4.5 },
                };

                foreach (var poi in popularPois)
                {
                    var poiCard = CreatePoiCard(poi);
                    PopularPoiContainer.Children.Add(poiCard);
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        private Frame CreatePoiCard(PoiItem poi)
        {
            var card = new Frame
            {
                BackgroundColor = Color.FromArgb("#FFF0F5"),
                CornerRadius = 12,
                Padding = 12,
                WidthRequest = 150,
                HasShadow = true
            };

            var stack = new StackLayout();
            
            var categoryLabel = new Label
            {
                Text = poi.Category,
                FontSize = 10,
                TextColor = Color.FromArgb("#FF69B4")
            };
            
            var nameLabel = new Label
            {
                Text = poi.Name,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#333333"),
                LineBreakMode = LineBreakMode.TailTruncation
            };
            
            var ratingLabel = new Label
            {
                Text = $"⭐ {poi.Rating:F1}",
                FontSize = 11,
                TextColor = Color.FromArgb("#666666")
            };

            stack.Children.Add(categoryLabel);
            stack.Children.Add(nameLabel);
            stack.Children.Add(ratingLabel);
            
            card.Content = stack;
            
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => 
            {
                await Navigation.PushAsync(new AudioPlayerPage(poi.Id, poi.Name));
            };
            card.GestureRecognizers.Add(tapGesture);
            
            return card;
        }

        private void MapButton_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new MapPage());
        }

        private async void QrScanButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new QrScanPage());
        }

        private void TourButton_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new TourManagerPage());
        }

        private void PoiListButton_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new PoiListPage());
        }

        private async void AudioButton_Clicked(object sender, EventArgs e)
        {
            if (!AppConfig.IsLoggedIn)
            {
                await DisplayAlert("⚠️", NO_LOGIN_REQUIRED, "OK");
                return;
            }
            await Navigation.PushAsync(new AudioPlayerPage());
        }

        private void SettingsButton_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new SettingsPage());
        }

        private async void VietnameseButton_Clicked(object sender, EventArgs e)
        {
            if (AppConfig.CurrentLanguage == "vi")
                return;
                
            var result = await DisplayAlert(LANGUAGE_DIALOG, CHANGE_LANGUAGE, ENGLISH, CANCEL);
            if (result)
            {
                AppConfig.CurrentLanguage = "en";
                UpdateLanguageButtons();
            }
        }

        private async void EnglishButton_Clicked(object sender, EventArgs e)
        {
            if (!FeatureGate.HasEnglishAudio)
            {
                await DisplayAlert("💎 Premium", "🔓 Tính năng audio tiếng Anh yêu cầu gói Premium", "OK");
                return;
            }
            
            if (AppConfig.CurrentLanguage == "en")
                return;
                
            AppConfig.CurrentLanguage = "en";
            UpdateLanguageButtons();
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

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Refresh data when page appears
            _ = LoadStatsAsync();
        }
    }

    public class PoiItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public double Rating { get; set; }
        public double Distance { get; set; }
    }
}
