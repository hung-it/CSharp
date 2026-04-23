using ZXing.Net.Maui;

namespace VinhKhanhAudioGuide.App;

public partial class QrScanPage : ContentPage
{
    private bool _isProcessing = false;
    private string _resolvedUserId = string.Empty;
    private bool _cameraStarted = false;

    public QrScanPage()
    {
        InitializeComponent();

        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple = false
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isProcessing = false;

        try
        {
            _resolvedUserId = await AppConfig.ResolveDefaultUserIdAsync();
        }
        catch
        {
            _resolvedUserId = string.Empty;
        }

        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlertAsync("📷 Cần quyền camera",
                "🔒 Vui lòng cấp quyền camera để quét mã QR.", "OK");
            try { await Shell.Current.GoToAsync(".."); } catch { }
            return;
        }

        await Task.Delay(300);

        try
        {
            BarcodeReader.IsDetecting = true;
            _cameraStarted = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Camera start error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _cameraStarted = false;
        try { BarcodeReader.IsDetecting = false; } catch { }
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessing || !_cameraStarted)
            return;

        var result = e.Results?.FirstOrDefault();
        if (result?.Value is null or "")
            return;

        _isProcessing = true;

        try { BarcodeReader.IsDetecting = false; } catch { }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var qrPayload = result!.Value!.Trim();

            if (string.IsNullOrWhiteSpace(_resolvedUserId) || !Guid.TryParse(_resolvedUserId, out _))
            {
                await DisplayAlertAsync("❌ Lỗi",
                    "⚠️ Không thể xác thực người dùng. Vui lòng kiểm tra kết nối.", "OK");
                try { await Shell.Current.GoToAsync(".."); } catch { }
                return;
            }

            try
            {
                await Shell.Current.GoToAsync("..");
                await Task.Delay(200);
                await Shell.Current.GoToAsync(
                    $"audio?qr={Uri.EscapeDataString(qrPayload)}&userId={Uri.EscapeDataString(_resolvedUserId)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                await DisplayAlertAsync("❌ Lỗi",
                    $"⚠️ Không thể mở trình phát audio: {ex.Message}", "OK");
            }
        });
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch { }
    }
}
