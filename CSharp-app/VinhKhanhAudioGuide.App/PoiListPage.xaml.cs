using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

public partial class PoiListPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public PoiListPage()
    {
        InitializeComponent();
        _httpClient = new HttpClient { BaseAddress = new Uri(AppConfig.ApiBaseUrl) };
        LoadPois();
    }

    private async void LoadPois()
    {
        try
        {
            var pois = await _httpClient.GetFromJsonAsync<List<PoiDetail>>("pois");
            PoiCollectionView.ItemsSource = pois ?? new List<PoiDetail>();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể tải danh sách POI: {ex.Message}", "OK");
        }
    }

    private async void OnPoiSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not PoiDetail poi)
            return;

        // Reset selection để có thể bấm lại cùng item
        PoiCollectionView.SelectedItem = null;

        await Shell.Current.GoToAsync(
            $"poiDetail" +
            $"?poiId={poi.Id}" +
            $"&name={Uri.EscapeDataString(poi.Name ?? string.Empty)}" +
            $"&desc={Uri.EscapeDataString(poi.Description ?? string.Empty)}" +
            $"&district={Uri.EscapeDataString(poi.District ?? string.Empty)}" +
            $"&code={Uri.EscapeDataString(poi.Code ?? string.Empty)}" +
            $"&lat={poi.Latitude}" +
            $"&lng={poi.Longitude}" +
            $"&imageUrl={Uri.EscapeDataString(poi.ImageUrl ?? string.Empty)}" +
            $"&mapLink={Uri.EscapeDataString(poi.MapLink ?? string.Empty)}");
    }
}

public class PoiDetail
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? District { get; set; }
    public string? ImageUrl { get; set; }
    public string? MapLink { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
