namespace VinhKhanhAudioGuide.App;

[QueryProperty(nameof(PoiId), "poiId")]
[QueryProperty(nameof(PoiName), "name")]
[QueryProperty(nameof(PoiDescription), "desc")]
[QueryProperty(nameof(PoiDistrict), "district")]
[QueryProperty(nameof(PoiCode), "code")]
[QueryProperty(nameof(PoiLat), "lat")]
[QueryProperty(nameof(PoiLng), "lng")]
[QueryProperty(nameof(PoiImageUrl), "imageUrl")]
[QueryProperty(nameof(PoiMapLink), "mapLink")]
public partial class PoiDetailPage : ContentPage
{
    private string _poiCode = string.Empty;
    private string _mapLink = string.Empty;

    public string PoiId { get; set; } = string.Empty;

    public string PoiName
    {
        set
        {
            var name = Uri.UnescapeDataString(value ?? string.Empty);
            NameLabel.Text = name;
            Title = name;
        }
    }

    public string PoiDescription
    {
        set => DescLabel.Text = Uri.UnescapeDataString(value ?? string.Empty);
    }

    public string PoiDistrict
    {
        set => DistrictLabel.Text = Uri.UnescapeDataString(value ?? string.Empty);
    }

    public string PoiCode
    {
        set
        {
            _poiCode = Uri.UnescapeDataString(value ?? string.Empty);
            CodeLabel.Text = $"Mã: {_poiCode}";
        }
    }

    public string PoiLat { get; set; } = string.Empty;
    public string PoiLng { get; set; } = string.Empty;

    public string PoiImageUrl
    {
        set
        {
            var url = Uri.UnescapeDataString(value ?? string.Empty);
            if (!string.IsNullOrEmpty(url))
                PoiImage.Source = ImageSource.FromUri(new Uri(url));
        }
    }

    public string PoiMapLink
    {
        set => _mapLink = Uri.UnescapeDataString(value ?? string.Empty);
    }

    // Setter cuối — sau khi cả lat lẫn lng đã được set
    public string PoiLngFinal
    {
        set
        {
            PoiLng = value;
            UpdateCoordLabel();
        }
    }

    public PoiDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateCoordLabel();
    }

    private void UpdateCoordLabel()
    {
        if (!string.IsNullOrEmpty(PoiLat) && !string.IsNullOrEmpty(PoiLng))
            CoordLabel.Text = $"{PoiLat}, {PoiLng}";
    }

    private async void OnListenClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_poiCode))
        {
            await DisplayAlertAsync("Lỗi", "Không tìm thấy mã POI", "OK");
            return;
        }

        var userId = await AppConfig.ResolveDefaultUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            await DisplayAlertAsync("Lỗi kết nối", "Không resolve được user từ backend. Vui lòng kiểm tra backend đang chạy.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(
            $"audio?qr={Uri.EscapeDataString(_poiCode)}&userId={userId}");
    }

    private async void OnMapLinkClicked(object? sender, EventArgs e)
    {
        var url = !string.IsNullOrEmpty(_mapLink)
            ? _mapLink
            : $"https://maps.google.com/?q={PoiLat},{PoiLng}";

        await Launcher.OpenAsync(url);
    }
}
