using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

public partial class MapPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private List<PoiData> _allPois = new();

    public MapPage()
    {
        InitializeComponent();
        _httpClient = AppConfig.CreateHttpClient();

        // Default: Trường Đại học Sài Gòn (cơ sở 1)
        map.MoveToRegion(MapSpan.FromCenterAndRadius(
            new Location(10.7553, 106.6602),
            Distance.FromKilometers(2)));

        LoadPoisAndDisplayOnMap();
    }

    private async void LoadPoisAndDisplayOnMap()
    {
        try
        {
            var pois = await _httpClient.GetFromJsonAsync<List<PoiData>>("pois");

            if (pois == null || pois.Count == 0)
            {
                await DisplayAlertAsync("Info", "No POIs found", "OK");
                return;
            }

            _allPois = pois;

            foreach (var poi in _allPois)
            {
                double lat = poi.Latitude != 0 ? poi.Latitude : GetDefaultLatitude(poi.Name);
                double lng = poi.Longitude != 0 ? poi.Longitude : GetDefaultLongitude(poi.Name);

                var pin = new Pin
                {
                    Label = poi.Name,
                    Address = poi.Description ?? poi.District ?? string.Empty,
                    Location = new Location(lat, lng)
                };

                // Capture poi cho closure
                var capturedPoi = poi;
                var capturedLat = lat;
                var capturedLng = lng;

                pin.MarkerClicked += async (s, e) =>
                {
                    e.HideInfoWindow = false;
                    await Shell.Current.GoToAsync(
                        $"poiDetail" +
                        $"?poiId={capturedPoi.Id}" +
                        $"&name={Uri.EscapeDataString(capturedPoi.Name ?? string.Empty)}" +
                        $"&desc={Uri.EscapeDataString(capturedPoi.Description ?? string.Empty)}" +
                        $"&district={Uri.EscapeDataString(capturedPoi.District ?? string.Empty)}" +
                        $"&code={Uri.EscapeDataString(capturedPoi.Code ?? string.Empty)}" +
                        $"&lat={capturedLat}" +
                        $"&lng={capturedLng}" +
                        $"&imageUrl={Uri.EscapeDataString(capturedPoi.ImageUrl ?? string.Empty)}" +
                        $"&mapLink={Uri.EscapeDataString(capturedPoi.MapLink ?? string.Empty)}");
                };

                map.Pins.Add(pin);
            }

            NearbyPoisCollectionView.ItemsSource = _allPois;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load POIs: {ex.Message}", "OK");
        }
    }

    // Tọa độ thực từ DataSeeder (CSharp-main)
    private static readonly Dictionary<string, (double Lat, double Lng)> _knownPois = new(StringComparer.OrdinalIgnoreCase)
    {
        // Xóm Chiếu
        ["Quán Bánh Mì Xóm Chiếu"] = (10.7530, 106.6878),
        ["Chợ Xóm Chiếu"]          = (10.7540, 106.6880),
        ["Đình Xóm Chiếu"]         = (10.7550, 106.6882),
        ["Nhà Cổ Xóm Chiếu"]       = (10.7524, 106.6869),
        // Vĩnh Hội
        ["Nhà Thờ Vĩnh Hội"]       = (10.7600, 106.6900),
        ["Khách Sạn Vĩnh Hội"]     = (10.7610, 106.6910),
        ["Cây Cổ Thụ Vĩnh Hội"]   = (10.7620, 106.6920),
        ["Hẻm Ẩm Thực Vĩnh Hội"]  = (10.7617, 106.6896),
        ["Công Viên Bờ Kênh"]      = (10.7632, 106.6931),
        // Khánh Hội
        ["Tiệm Cơm Khánh Hội"]    = (10.7670, 106.6950),
        ["Chùa Khánh Hội"]         = (10.7680, 106.6960),
        ["Hồ Cá Khánh Hội"]       = (10.7690, 106.6970),
        ["Bến Tàu Khánh Hội"]     = (10.7661, 106.6984),
        ["Chợ Đêm Khánh Hội"]     = (10.7702, 106.6968),
    };

    private static double GetDefaultLatitude(string name)
        => _knownPois.TryGetValue(name, out var c) ? c.Lat : 10.7553;

    private static double GetDefaultLongitude(string name)
        => _knownPois.TryGetValue(name, out var c) ? c.Lng : 106.6602;
}

public class PoiData
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
