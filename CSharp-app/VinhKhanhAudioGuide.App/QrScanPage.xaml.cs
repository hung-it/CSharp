using ZXing.Net.Maui;

namespace VinhKhanhAudioGuide.App;

public partial class QrScanPage : ContentPage
{
    private bool _isProcessing = false;
    private string _resolvedUserId = string.Empty;

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

        // Resolve userId thực
        _resolvedUserId = await AppConfig.ResolveDefaultUserIdAsync();
        if (string.IsNullOrWhiteSpace(_resolvedUserId))
        {
            await DisplayAlertAsync("Lỗi kết nối", "Không resolve được user từ backend. Vui lòng kiểm tra backend đang chạy.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlertAsync("Cần quyền camera",
                "Vui lòng cấp quyền camera để quét mã QR.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Dừng camera khi rời trang
        BarcodeReader.IsDetecting = false;
    }

    private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        // Chỉ xử lý 1 lần, tránh trigger nhiều lần liên tiếp
        if (_isProcessing) return;
        _isProcessing = true;

        var result = e.Results.FirstOrDefault();
        if (result is null)
        {
            _isProcessing = false;
            return;
        }

        var qrPayload = result.Value?.Trim();
        if (string.IsNullOrEmpty(qrPayload))
        {
            _isProcessing = false;
            return;
        }

        // Dừng camera ngay
        BarcodeReader.IsDetecting = false;

        // Navigate phải chạy trên main thread
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Shell.Current.GoToAsync("..");
            await Shell.Current.GoToAsync(
                $"audio?qr={Uri.EscapeDataString(qrPayload)}&userId={_resolvedUserId}");
        });
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
