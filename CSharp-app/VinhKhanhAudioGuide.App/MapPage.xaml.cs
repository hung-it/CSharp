using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;

namespace VinhKhanhAudioGuide.App
{
    public partial class MapPage : ContentPage
    {
        private const string GPS_PERMISSION_TITLE = "📍 Quyền GPS";
        private const string GPS_PERMISSION_MESSAGE = "Ứng dụng cần quyền truy cập vị trí để hiển thị bản đồ và điểm gần bạn. Bạn có muốn cấp quyền không?";
        private const string GPS_PERMISSION_DENIED = "⚠️ Cần cấp quyền GPS để sử dụng tính năng này";
        private const string GPS_PERMISSION_ERROR = "❌ Lỗi khi yêu cầu quyền GPS";
        private const string GPS_LOCATING = "⏳ Đang định vị...";
        private const string GPS_NOT_SUPPORTED = "❌ GPS không hỗ trợ trên thiết bị này";
        private const string GPS_UNAVAILABLE = "❌ GPS không khả dụng";
        private const string GPS_SUCCESS = "✅ Đã xác định vị trí";
        private const string GPS_FAILED = "⚠️ Không thể xác định vị trí";
        private const string LOCATION_UNKNOWN = "Chưa xác định";
        private const string YES = "Có";
        private const string NO = "Không";
        private const string OK = "OK";
        private const string REFRESHING = "🔄 Đang làm mới...";
        private const string REFRESH_COMPLETE = "✅ Đã làm mới";
        private const string REFRESH_FAILED = "❌ Lỗi làm mới";

        private double currentLatitude = 0;
        private double currentLongitude = 0;
        private bool isGpsEnabled = false;

        public MapPage()
        {
            InitializeComponent();
            InitializeGpsAsync();
        }

        private async void InitializeGpsAsync()
        {
            GpsStatusLabel.Text = GPS_LOCATING;
            GpsDetailLabel.Text = "Đang khởi tạo GPS...";
            GpsActivityIndicator.IsRunning = true;

            try
            {
                // Check if GPS is available
                var isLocationSupported = Microsoft.Maui.Devices.Sensors.Location.Default.IsSupported;
                
                if (!isLocationSupported)
                {
                    GpsStatusLabel.Text = GPS_NOT_SUPPORTED;
                    GpsStatusLabel.TextColor = Colors.Red;
                    GpsDetailLabel.Text = "Thiết bị không hỗ trợ GPS";
                    GpsActivityIndicator.IsRunning = false;
                    return;
                }

                // Request permission
                var permissionGranted = await RequestLocationPermissionAsync();
                
                if (!permissionGranted)
                {
                    GpsStatusLabel.Text = GPS_PERMISSION_DENIED;
                    GpsStatusLabel.TextColor = Colors.Orange;
                    GpsDetailLabel.Text = "Vui lòng bật quyền truy cập vị trí";
                    GpsActivityIndicator.IsRunning = false;
                    return;
                }

                // Get current location
                await GetCurrentLocationAsync();
            }
            catch (Exception ex)
            {
                GpsStatusLabel.Text = GPS_FAILED;
                GpsStatusLabel.TextColor = Colors.Red;
                GpsDetailLabel.Text = ex.Message;
                GpsActivityIndicator.IsRunning = false;
            }
        }

        private async Task<bool> RequestLocationPermissionAsync()
        {
            try
            {
                var status = await Microsoft.Maui.Essentials.Permissions.CheckStatusAsync<Microsoft.Maui.Essentials.Permissions.LocationWhenInUse>();
                
                if (status == Microsoft.Maui.Essentials.PermissionStatus.Granted)
                {
                    return true;
                }

                // Try to request permission
                status = await Microsoft.Maui.Essentials.Permissions.RequestAsync<Microsoft.Maui.Essentials.Permissions.LocationWhenInUse>();
                
                return status == Microsoft.Maui.Essentials.PermissionStatus.Granted;
            }
            catch
            {
                return false;
            }
        }

        private async Task GetCurrentLocationAsync()
        {
            GpsActivityIndicator.IsRunning = true;
            GpsDetailLabel.Text = "Đang lấy tọa độ...";

            try
            {
                var request = new Microsoft.Maui.Devices.Sensors.GeolocationRequest(
                    Microsoft.Maui.Devices.Sensors.GeolocationAccuracy.Medium);
                
                var location = await Microsoft.Maui.Devices.Sensors.Geolocation.GetLocationAsync(request);
                
                if (location != null)
                {
                    currentLatitude = location.Latitude;
                    currentLongitude = location.Longitude;
                    
                    GpsStatusLabel.Text = GPS_SUCCESS;
                    GpsStatusLabel.TextColor = Colors.LightGreen;
                    GpsDetailLabel.Text = $"📍 {currentLatitude:F6}, {currentLongitude:F6}";
                    CurrentLocationLabel.Text = $"📍 Vị trí hiện tại: {currentLatitude:F4}, {currentLongitude:F4}";
                    
                    isGpsEnabled = true;
                    
                    // Load nearby POIs
                    await LoadNearbyPoisAsync();
                }
                else
                {
                    GpsStatusLabel.Text = GPS_FAILED;
                    GpsStatusLabel.TextColor = Colors.Orange;
                    GpsDetailLabel.Text = "Không thể lấy vị trí";
                }
            }
            catch (Exception ex)
            {
                GpsStatusLabel.Text = GPS_UNAVAILABLE;
                GpsStatusLabel.TextColor = Colors.Red;
                GpsDetailLabel.Text = ex.Message;
            }
            finally
            {
                GpsActivityIndicator.IsRunning = false;
            }
        }

        private async Task LoadNearbyPoisAsync()
        {
            try
            {
                // Sample nearby POIs based on current location
                var nearbyPois = new List<PoiItem>
                {
                    new PoiItem { Id = "1", Name = "Quán Vĩnh Hạnh", Category = "🏠 Nhà hàng", Distance = 0.3 },
                    new PoiItem { Id = "2", Name = "Bánh xèo Tư Hòa", Category = "🍜 Ẩm thực", Distance = 0.5 },
                    new PoiItem { Id = "3", Name = "Chùa Vĩnh Nghiêm", Category = "🏛️ Di tích", Distance = 0.8 },
                };

                NearbyPoiContainer.Children.Clear();
                
                foreach (var poi in nearbyPois)
                {
                    var poiCard = CreateNearbyPoiCard(poi);
                    NearbyPoiContainer.Children.Add(poiCard);
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        private Frame CreateNearbyPoiCard(PoiItem poi)
        {
            var card = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 12,
                Padding = 12,
                WidthRequest = 140,
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
            
            var distanceLabel = new Label
            {
                Text = $"📍 {poi.Distance:F1} km",
                FontSize = 11,
                TextColor = Color.FromArgb("#666666")
            };

            stack.Children.Add(categoryLabel);
            stack.Children.Add(nameLabel);
            stack.Children.Add(distanceLabel);
            
            card.Content = stack;
            
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => 
            {
                await Navigation.PushAsync(new AudioPlayerPage(poi.Id, poi.Name));
            };
            card.GestureRecognizers.Add(tapGesture);
            
            return card;
        }

        private async void RefreshButton_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            button.Text = REFRESHING;
            button.IsEnabled = false;

            try
            {
                await GetCurrentLocationAsync();
                button.Text = REFRESH_COMPLETE;
                await Task.Delay(1000);
            }
            catch
            {
                button.Text = REFRESH_FAILED;
                await Task.Delay(1000);
            }
            finally
            {
                button.Text = "🔄 Làm mới";
                button.IsEnabled = true;
            }
        }

        private async void QrScanButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new QrScanPage());
        }

        private void PoiListButton_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new PoiListPage());
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (isGpsEnabled)
            {
                _ = GetCurrentLocationAsync();
            }
        }
    }
}
