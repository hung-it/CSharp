using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

[QueryProperty(nameof(TourId), "tourId")]
[QueryProperty(nameof(TourName), "name")]
[QueryProperty(nameof(TourDesc), "desc")]
public partial class TourDetailPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private string _tourId = string.Empty;

    public string TourId
    {
        set
        {
            _tourId = value ?? string.Empty;
            if (!string.IsNullOrEmpty(_tourId))
                MainThread.BeginInvokeOnMainThread(() => _ = LoadStopsAsync());
        }
    }

    public string TourName
    {
        set
        {
            var name = Uri.UnescapeDataString(value ?? string.Empty);
            TourNameLabel.Text = name;
            Title = name;
        }
    }

    public string TourDesc
    {
        set => TourDescLabel.Text = Uri.UnescapeDataString(value ?? string.Empty);
    }

    public TourDetailPage()
    {
        InitializeComponent();
        _httpClient = new HttpClient { BaseAddress = new Uri(AppConfig.ApiBaseUrl) };
    }

    private async Task LoadStopsAsync()
    {
        try
        {
            var stops = await _httpClient.GetFromJsonAsync<List<TourStopItem>>(
                $"tours/{_tourId}/stops");

            if (stops == null || stops.Count == 0)
            {
                EmptyLabel.IsVisible = true;
                return;
            }

            // Map sang ViewModel
            var items = stops
                .OrderBy(s => s.Sequence)
                .Select(s => new TourStopViewModel
                {
                    Sequence    = s.Sequence,
                    NextStopHint = s.NextStopHint ?? string.Empty,
                    HasHint     = !string.IsNullOrEmpty(s.NextStopHint),
                    PoiId       = s.Poi?.Id.ToString() ?? string.Empty,
                    PoiCode     = s.Poi?.Code ?? string.Empty,
                    PoiName     = s.Poi?.Name ?? "(Chưa có tên)",
                    PoiDistrict = s.Poi?.District ?? string.Empty,
                    PoiDesc     = s.Poi?.Description ?? string.Empty,
                    PoiLat      = s.Poi?.Latitude ?? 0,
                    PoiLng      = s.Poi?.Longitude ?? 0,
                    PoiImageUrl = s.Poi?.ImageUrl ?? string.Empty,
                    PoiMapLink  = s.Poi?.MapLink ?? string.Empty,
                })
                .ToList();

            StopsCollectionView.ItemsSource = items;
            StopCountLabel.Text = $"{items.Count} điểm dừng";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể tải stops: {ex.Message}", "OK");
        }
    }

    private async void OnStopSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TourStopViewModel stop)
            return;

        StopsCollectionView.SelectedItem = null;

        await Shell.Current.GoToAsync(
            $"poiDetail" +
            $"?poiId={stop.PoiId}" +
            $"&name={Uri.EscapeDataString(stop.PoiName)}" +
            $"&desc={Uri.EscapeDataString(stop.PoiDesc)}" +
            $"&district={Uri.EscapeDataString(stop.PoiDistrict)}" +
            $"&code={Uri.EscapeDataString(stop.PoiCode)}" +
            $"&lat={stop.PoiLat}" +
            $"&lng={stop.PoiLng}" +
            $"&imageUrl={Uri.EscapeDataString(stop.PoiImageUrl)}" +
            $"&mapLink={Uri.EscapeDataString(stop.PoiMapLink)}");
    }
}

// API response models
public class TourStopItem
{
    public Guid Id { get; set; }
    public int Sequence { get; set; }
    public string? NextStopHint { get; set; }
    public TourStopPoi? Poi { get; set; }
}

public class TourStopPoi
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? District { get; set; }
    public string? ImageUrl { get; set; }
    public string? MapLink { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

// ViewModel cho CollectionView
public class TourStopViewModel
{
    public int Sequence { get; set; }
    public string NextStopHint { get; set; } = string.Empty;
    public bool HasHint { get; set; }
    public string PoiId { get; set; } = string.Empty;
    public string PoiCode { get; set; } = string.Empty;
    public string PoiName { get; set; } = string.Empty;
    public string PoiDistrict { get; set; } = string.Empty;
    public string PoiDesc { get; set; } = string.Empty;
    public double PoiLat { get; set; }
    public double PoiLng { get; set; }
    public string PoiImageUrl { get; set; } = string.Empty;
    public string PoiMapLink { get; set; } = string.Empty;
}
