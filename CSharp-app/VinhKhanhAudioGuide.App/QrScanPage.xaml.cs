using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Essentials;

namespace VinhKhanhAudioGuide.App
{
    public partial class QrScanPage : ContentPage
    {
        private const string CAMERA_PERMISSION_TITLE = "📷 Cần quyền camera";
        private const string CAMERA_PERMISSION_MESSAGE = "Ứng dụng cần quyền truy cập camera để quét mã QR. Bạn có muốn cấp quyền không?";
        private const string CAMERA_DENIED = "❌ Không thể quét QR vì không có quyền camera";
        private const string ERROR_TITLE = "❌ Lỗi";
        private const string SCANNING = "⏳ Đang quét...";
        private const string SCAN_SUCCESS = "✅ Đã quét thành công!";
        private const string POI_FOUND = "📍 Điểm: {0}";
        private const string REDIRECTING = "⏳ Đang chuyển hướng...";
        private const string QR_NOT_FOUND = "😕 Không tìm thấy thông tin cho mã QR này";
        private const string INVALID_QR = "❌ Mã QR không hợp lệ";
        private const string YES = "Có";
        private const string NO = "Không";
        private const string OK = "OK";
        private const string CANCEL = "Hủy";

        private bool isScanning = true;

        public QrScanPage()
        {
            InitializeComponent();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            UpdateStatus(SCANNING);
            
            try
            {
                // Check camera permission
                var hasPermission = await CheckCameraPermissionAsync();
                
                if (!hasPermission)
                {
                    var granted = await RequestCameraPermissionAsync();
                    
                    if (!granted)
                    {
                        UpdateStatus(CAMERA_DENIED);
                        await DisplayAlert(ERROR_TITLE, CAMERA_DENIED, OK);
                        return;
                    }
                }

                // Start scanning simulation
                await StartScanningAsync();
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Lỗi: {ex.Message}");
            }
        }

        private async Task<bool> CheckCameraPermissionAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                return status == PermissionStatus.Granted;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> RequestCameraPermissionAsync()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Camera>();
                return status == PermissionStatus.Granted;
            }
            catch
            {
                return false;
            }
        }

        private async Task StartScanningAsync()
        {
            // Simulate scanning process
            UpdateStatus(SCANNING);
            LoadingIndicator.IsRunning = true;

            try
            {
                // In a real app, this would use a QR scanner library
                // For demo, we'll simulate finding a POI after a delay
                await Task.Delay(3000);

                if (!isScanning)
                    return;

                // Simulate successful scan
                await OnQrCodeScannedAsync("POI-001");
            }
            catch (Exception)
            {
                // Ignore cancellation
            }
        }

        private async Task OnQrCodeScannedAsync(string poiId)
        {
            isScanning = false;
            UpdateStatus(SCAN_SUCCESS);
            LoadingIndicator.IsRunning = false;

            try
            {
                // Get POI info
                var poiName = GetPoiNameFromId(poiId);
                
                if (!string.IsNullOrEmpty(poiName))
                {
                    UpdateStatus(string.Format(POI_FOUND, poiName));
                    await Task.Delay(1000);
                    
                    UpdateStatus(REDIRECTING);
                    await Task.Delay(500);
                    
                    // Navigate to audio player
                    await Navigation.PushAsync(new AudioPlayerPage(poiId, poiName));
                }
                else
                {
                    UpdateStatus(QR_NOT_FOUND);
                    await DisplayAlert("❌", QR_NOT_FOUND, OK);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Lỗi: {ex.Message}");
            }
        }

        private string GetPoiNameFromId(string poiId)
        {
            // Sample POI mapping
            var poiMap = new System.Collections.Generic.Dictionary<string, string>
            {
                { "POI-001", "Quán ăn Vĩnh Hạnh" },
                { "POI-002", "Bánh xèo Tư Hòa" },
                { "POI-003", "Chùa Vĩnh Nghiêm" },
                { "POI-004", "Cà phê Sữa Đá" },
            };

            return poiMap.ContainsKey(poiId) ? poiMap[poiId] : null;
        }

        private void UpdateStatus(string status)
        {
            StatusLabel.Text = status;
            
            if (status.Contains("✅"))
            {
                StatusLabel.TextColor = Color.FromArgb("#4CAF50");
            }
            else if (status.Contains("❌"))
            {
                StatusLabel.TextColor = Colors.Red;
            }
            else if (status.Contains("⏳"))
            {
                StatusLabel.TextColor = Color.FromArgb("#FF69B4");
            }
            else
            {
                StatusLabel.TextColor = Color.FromArgb("#CCCCCC");
            }
        }

        private async void CloseButton_Clicked(object sender, EventArgs e)
        {
            isScanning = false;
            await Navigation.PopAsync();
        }

        private async void ManualButton_Clicked(object sender, EventArgs e)
        {
            isScanning = false;
            
            var result = await DisplayPromptAsync("🔔", "Nhập mã POI hoặc tên điểm tham quan:", "Tìm kiếm", CANCEL, "Ví dụ: POI-001");
            
            if (!string.IsNullOrEmpty(result))
            {
                await OnQrCodeScannedAsync(result);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            isScanning = false;
        }
    }
}
