using System.Net.Http.Json;

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
[QueryProperty(nameof(TriggerSource), "trigger")]
[QueryProperty(nameof(PageSource), "source")]
public partial class PoiDetailPage : ContentPage
{
    private string _poiCode = string.Empty;
    private string _mapLink = string.Empty;
    private Guid? _visitId;

    public string PoiId { get; set; } = string.Empty;
    public string TriggerSource { get; set; } = "Map";
    public string PageSource { get; set; } = "Map";

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

    public PoiDetailPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateCoordLabel();
        await StartVisitTrackingAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await TrackingService.EndVisitAsync();
    }

    private async Task StartVisitTrackingAsync()
    {
        if (string.IsNullOrEmpty(PoiId) || !Guid.TryParse(PoiId, out var poiGuid))
            return;

        try
        {
            var userId = await AppConfig.ResolveDefaultUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userGuid))
                return;

            double? lat = null, lng = null;
            if (double.TryParse(PoiLat, out var parsedLat))
                lat = parsedLat;
            if (double.TryParse(PoiLng, out var parsedLng))
                lng = parsedLng;

            _visitId = await TrackingService.StartVisitAsync(
                userGuid,
                poiGuid,
                TriggerSource,
                PageSource,
                lat,
                lng);
        }
        catch
        {
            // Silent fail - tracking is not critical
        }
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

        try
        {
            var userId = await AppConfig.ResolveDefaultUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId))
            {
                // Fallback: try direct resolve
                var httpClient = AppConfig.CreateHttpClient();
                var response = await httpClient.PostAsJsonAsync("users/resolve", new
                {
                    Username = "demo",
                    PreferredLanguage = "vi",
                    Password = "1"
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                    userId = result?.Id.ToString() ?? "00000000-0000-0000-0000-000000000001";
                }
                else
                {
                    userId = "00000000-0000-0000-0000-000000000001";
                }
            }

            // Record that user is starting to listen
            TrackingService.RecordListeningStart();

            await Shell.Current.GoToAsync(
                $"audio?qr={Uri.EscapeDataString(_poiCode)}&userId={userId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Listen error: {ex.Message}");
            // Use fallback user ID
            TrackingService.RecordListeningStart();
            await Shell.Current.GoToAsync(
                $"audio?qr={Uri.EscapeDataString(_poiCode)}&userId=00000000-0000-0000-0000-000000000001");
        }
    }

    private async void OnMapLinkClicked(object? sender, EventArgs e)
    {
        var url = !string.IsNullOrEmpty(_mapLink)
            ? _mapLink
            : $"https://maps.google.com/?q={PoiLat},{PoiLng}";

        await Launcher.OpenAsync(url);
    }
}
