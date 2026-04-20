namespace VinhKhanhAudioGuide.App;

using System.Net.Http.Json;

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

    private async void OnListenClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_poiCode))
        {
            await DisplayAlert("Lỗi", "Không tìm thấy mã POI", "OK");
            return;
        }

        // Lấy userId thực từ API thay vì hardcode
        var userId = await ResolveUserIdAsync();
        await Shell.Current.GoToAsync(
            $"audio?qr={Uri.EscapeDataString(_poiCode)}&userId={userId}");
    }

    private static async Task<string> ResolveUserIdAsync()
    {
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(AppConfig.ApiBaseUrl) };
            var response = await http.PostAsJsonAsync("users/resolve", new
            {
                ExternalRef = "USER_DEMO",
                PreferredLanguage = "vi"
            });
            if (response.IsSuccessStatusCode)
            {
                var user = await response.Content.ReadFromJsonAsync<ResolvedUserInfo>();
                return user?.Id.ToString() ?? string.Empty;
            }
        }
        catch { }
        return string.Empty;
    }

    private async void OnMapLinkClicked(object sender, EventArgs e)
    {
        var url = !string.IsNullOrEmpty(_mapLink)
            ? _mapLink
            : $"https://maps.google.com/?q={PoiLat},{PoiLng}";

        await Launcher.OpenAsync(url);
    }
}

public class ResolvedUserInfo
{
    public Guid Id { get; set; }
    public string ExternalRef { get; set; } = string.Empty;
}
