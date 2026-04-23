using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

[QueryProperty(nameof(TourId), "tourId")]
[QueryProperty(nameof(TourName), "name")]
[QueryProperty(nameof(TourDesc), "desc")]
public partial class TourDetailPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private string _tourId = string.Empty;
    private Guid? _tourViewId;
    private int _poiVisitedCount = 0;
    private int _audioListenedCount = 0;

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
        _httpClient = AppConfig.CreateHttpClient();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await StartTourViewTrackingAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await TrackingService.EndTourViewAsync(_poiVisitedCount, _audioListenedCount);
    }

    private async Task StartTourViewTrackingAsync()
    {
        if (string.IsNullOrEmpty(_tourId) || !Guid.TryParse(_tourId, out var tourGuid))
            return;

        try
        {
            var userId = await AppConfig.ResolveDefaultUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userGuid))
                return;

            _tourViewId = await TrackingService.StartTourViewAsync(userGuid, tourGuid);
        }
        catch
        {
            // Silent fail
        }
    }

    private async Task LoadStopsAsync()
    {
        try
        {
            LoadingOverlay.IsVisible = true;
            
            var stops = await _httpClient.GetFromJsonAsync<List<TourStopItem>>(
                $"tours/{_tourId}/stops");

            LoadingOverlay.IsVisible = false;

            if (stops == null || stops.Count == 0)
            {
                // Try to load from default stops if API returns empty
                EmptyLabel.IsVisible = true;
                StopsCollectionView.ItemsSource = GetDefaultStops();
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
        catch (HttpRequestException)
        {
            LoadingOverlay.IsVisible = false;
            // Server not reachable - show default stops
            EmptyLabel.IsVisible = true;
            StopsCollectionView.ItemsSource = GetDefaultStops();
            await DisplayAlertAsync("Thông báo", "Đang hoạt động offline. Hiển thị dữ liệu mặc định.", "OK");
        }
        catch (TaskCanceledException)
        {
            LoadingOverlay.IsVisible = false;
            EmptyLabel.IsVisible = true;
            StopsCollectionView.ItemsSource = GetDefaultStops();
        }
        catch (Exception ex)
        {
            LoadingOverlay.IsVisible = false;
            EmptyLabel.IsVisible = true;
            StopsCollectionView.ItemsSource = GetDefaultStops();
            System.Diagnostics.Debug.WriteLine($"LoadStops error: {ex.Message}");
        }
    }

    private List<TourStopViewModel> GetDefaultStops()
    {
        // Return sample stops for offline/demo mode
        return new List<TourStopViewModel>
        {
            new() { Sequence = 1, PoiName = "Quán Bánh Mì Đặc Biệt", PoiDistrict = "Xóm Chiếu", NextStopHint = "Rẽ phải vào hẻm", HasHint = true },
            new() { Sequence = 2, PoiName = "Tiệm Cơm Gia Đình", PoiDistrict = "Khánh Hội", NextStopHint = "Đi bộ 200m", HasHint = true },
            new() { Sequence = 3, PoiName = "Hẻm Ăn Vĩnh Hội", PoiDistrict = "Vĩnh Hội", NextStopHint = "Hẻm giữa khu dân cư", HasHint = true },
            new() { Sequence = 4, PoiName = "Quán Cà Phê Sân Đình", PoiDistrict = "Xóm Chiếu", NextStopHint = "View đẹp", HasHint = true },
            new() { Sequence = 5, PoiName = "Chợ Xóm Chiếu", PoiDistrict = "Xóm Chiếu", NextStopHint = "Qua ngã tư", HasHint = true },
            new() { Sequence = 6, PoiName = "Đình Xóm Chiếu", PoiDistrict = "Xóm Chiếu", NextStopHint = "Đình thờ cổ", HasHint = true },
        };
    }

    private async void OnStopSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TourStopViewModel stop)
            return;

        StopsCollectionView.SelectedItem = null;

        // Track that user visited a POI from this tour
        _poiVisitedCount++;

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
            $"&mapLink={Uri.EscapeDataString(stop.PoiMapLink)}" +
            $"&trigger=Tour" +
            $"&source=TourDetail");
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
