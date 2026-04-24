using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

public partial class PoiListPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private List<PoiDisplayItem> _allPois = new();
    private List<PoiDisplayItem> _filteredPois = new();
    private string _currentFilter = "all";
    private string _currentSort = "default";
    private string _searchText = "";
    private string _userId = string.Empty;
    private string _anonymousRef = $"ANON_{Guid.NewGuid():N}";
    private double _currentLat = 10.7553;
    private double _currentLng = 106.6602;

    public PoiListPage()
    {
        InitializeComponent();
        _httpClient = AppConfig.CreateHttpClient();
        
        SortPicker.SelectedIndex = 0;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ResolveUserAsync();
        await GetCurrentLocationAsync();
        await LoadPoisAsync();
    }

    private async Task ResolveUserAsync()
    {
        try
        {
            _userId = await AppConfig.ResolveDefaultUserIdAsync(_httpClient);
        }
        catch
        {
            _userId = _anonymousRef;
        }
    }

    private async Task GetCurrentLocationAsync()
    {
        try
        {
            var location = await Geolocation.Default.GetLastKnownLocationAsync();
            if (location != null)
            {
                _currentLat = location.Latitude;
                _currentLng = location.Longitude;
            }
        }
        catch { /* Use default */ }
    }

    private async Task LoadPoisAsync()
    {
        try
        {
            var pois = await _httpClient.GetFromJsonAsync<List<PoiData>>("pois");

            if (pois == null || pois.Count == 0)
            {
                pois = GetKnownPois();
            }

            _allPois = pois.Select(p => new PoiDisplayItem
            {
                Id = p.Id,
                Code = p.Code ?? "",
                Name = p.Name ?? "Không tên",
                Description = p.Description ?? "",
                District = p.District ?? "",
                Latitude = p.Latitude != 0 ? p.Latitude : GetDefaultLatitude(p.Name ?? ""),
                Longitude = p.Longitude != 0 ? p.Longitude : GetDefaultLongitude(p.Name ?? ""),
                DistanceText = CalculateDistance(
                    p.Latitude != 0 ? p.Latitude : GetDefaultLatitude(p.Name ?? ""),
                    p.Longitude != 0 ? p.Longitude : GetDefaultLongitude(p.Name ?? "")),
                DistanceMeters = CalculateDistanceMeters(
                    p.Latitude != 0 ? p.Latitude : GetDefaultLatitude(p.Name ?? ""),
                    p.Longitude != 0 ? p.Longitude : GetDefaultLongitude(p.Name ?? ""))
            }).ToList();

            ApplyFilter();
        }
        catch
        {
            _allPois = GetKnownPois().Select(p => new PoiDisplayItem
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name ?? "Không tên",
                Description = p.Description ?? "",
                District = p.District ?? "",
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                DistanceText = CalculateDistance(p.Latitude, p.Longitude),
                DistanceMeters = CalculateDistanceMeters(p.Latitude, p.Longitude)
            }).ToList();

            ApplyFilter();
        }
    }

    private List<PoiData> GetKnownPois()
    {
        // 12 POIs đúng như DataSeeder - chia theo 3 khu vực
        return new List<PoiData>
        {
            // Xóm Chiếu (4 POIs)
            new() { Id = Guid.NewGuid(), Code = "POI001", Name = "Quán Bánh Mì Đặc Biệt", Description = "Quán bánh mì nổi tiếng nhất vùng với công thức gia truyền", District = "Xóm Chiếu", Latitude = 10.7530, Longitude = 106.6878 },
            new() { Id = Guid.NewGuid(), Code = "POI005", Name = "Chợ Xóm Chiếu", Description = "Chợ truyền thống với nhiều đặc sản địa phương", District = "Xóm Chiếu", Latitude = 10.7540, Longitude = 106.6880 },
            new() { Id = Guid.NewGuid(), Code = "POI009", Name = "Đình Xóm Chiếu", Description = "Đình thờ cổ có lịch sử hơn 100 năm", District = "Xóm Chiếu", Latitude = 10.7550, Longitude = 106.6882 },
            new() { Id = Guid.NewGuid(), Code = "POI004", Name = "Quán Cà Phê Sân Đình", Description = "Quán cà phê sân đình view đẹp, nơi hội họp của người dân", District = "Xóm Chiếu", Latitude = 10.7548, Longitude = 106.6875 },
            // Vĩnh Hội (5 POIs)
            new() { Id = Guid.NewGuid(), Code = "POI003", Name = "Hẻm Ăn Vĩnh Hội", Description = "Khu hẻm ăn uống địa phương nổi tiếng với nhiều món ngon", District = "Vĩnh Hội", Latitude = 10.7617, Longitude = 106.6896 },
            new() { Id = Guid.NewGuid(), Code = "POI006", Name = "Nhà Thờ Vĩnh Hội", Description = "Nhà thờ Công giáo với kiến trúc Pháp đẹp mắt", District = "Vĩnh Hội", Latitude = 10.7600, Longitude = 106.6900 },
            new() { Id = Guid.NewGuid(), Code = "POI008", Name = "Cây Cổ Thụ", Description = "Cây cổ thụ hơn 100 năm tuổi, biểu tượng của khu phố", District = "Vĩnh Hội", Latitude = 10.7620, Longitude = 106.6920 },
            new() { Id = Guid.NewGuid(), Code = "POI011", Name = "Khách Sạn Vĩnh Hội", Description = "Khách sạn lịch sử từ thời Pháp thuộc", District = "Vĩnh Hội", Latitude = 10.7610, Longitude = 106.6910 },
            // Khánh Hội (3 POIs)
            new() { Id = Guid.NewGuid(), Code = "POI002", Name = "Tiệm Cơm Gia Đình", Description = "Tiệm cơm bình dân được yêu thích nhất khu vực", District = "Khánh Hội", Latitude = 10.7670, Longitude = 106.6950 },
            new() { Id = Guid.NewGuid(), Code = "POI007", Name = "Hồ Cá Cảnh", Description = "Hồ cá tự nhiên với cảnh quan đẹp", District = "Khánh Hội", Latitude = 10.7695, Longitude = 106.6970 },
            new() { Id = Guid.NewGuid(), Code = "POI010", Name = "Chùa An Lạc", Description = "Chùa Phật giáo với kiến trúc đẹp và không gian yên bình", District = "Khánh Hội", Latitude = 10.7680, Longitude = 106.6960 },
            new() { Id = Guid.NewGuid(), Code = "POI012", Name = "Chợ Đêm Khánh Hội", Description = "Khu chợ đêm sôi động về đêm với nhiều món ăn đường phố", District = "Khánh Hội", Latitude = 10.7702, Longitude = 106.6968 }
        };
    }

    private void ApplyFilter()
    {
        _filteredPois = _allPois.Where(p =>
        {
            // Filter by district
            var matchFilter = _currentFilter switch
            {
                "xomchieu" => p.District == "Xóm Chiếu",
                "vinhhoi" => p.District == "Vĩnh Hội",
                "khanhhoi" => p.District == "Khánh Hội",
                _ => true
            };

            // Filter by search text
            var matchSearch = string.IsNullOrEmpty(_searchText) ||
                p.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                p.District.Contains(_searchText, StringComparison.OrdinalIgnoreCase);

            return matchFilter && matchSearch;
        }).ToList();

        // Apply sort
        _filteredPois = _currentSort switch
        {
            "name" => _filteredPois.OrderBy(p => p.Name).ToList(),
            "distance" => _filteredPois.OrderBy(p => p.DistanceMeters).ToList(),
            "popular" => _filteredPois.OrderByDescending(p => p.ListenCount).ToList(),
            _ => _filteredPois
        };

        // Update UI
        PoiCollectionView.ItemsSource = _filteredPois;
        ResultCountLabel.Text = $"{_filteredPois.Count} điểm";
        EmptyState.IsVisible = _filteredPois.Count == 0;
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? "";
        ApplyFilter();
    }

    private void OnClearSearchClicked(object? sender, EventArgs e)
    {
        SearchEntry.Text = "";
        _searchText = "";
        ApplyFilter();
    }

    private void OnFilterAllClicked(object? sender, EventArgs e)
    {
        _currentFilter = "all";
        ResetFilterButtons();
        FilterAllBtn.BackgroundColor = Color.FromArgb("#EC4899");
        FilterAllBtn.TextColor = Colors.White;
        ApplyFilter();
    }

    private void OnFilterXomChieuClicked(object? sender, EventArgs e)
    {
        _currentFilter = "xomchieu";
        ResetFilterButtons();
        FilterXomChieuBtn.BackgroundColor = Color.FromArgb("#EC4899");
        FilterXomChieuBtn.TextColor = Colors.White;
        ApplyFilter();
    }

    private void OnFilterVinhHoiClicked(object? sender, EventArgs e)
    {
        _currentFilter = "vinhhoi";
        ResetFilterButtons();
        FilterVinhHoiBtn.BackgroundColor = Color.FromArgb("#EC4899");
        FilterVinhHoiBtn.TextColor = Colors.White;
        ApplyFilter();
    }

    private void OnFilterKhanhHoiClicked(object? sender, EventArgs e)
    {
        _currentFilter = "khanhhoi";
        ResetFilterButtons();
        FilterKhanhHoiBtn.BackgroundColor = Color.FromArgb("#EC4899");
        FilterKhanhHoiBtn.TextColor = Colors.White;
        ApplyFilter();
    }

    private void ResetFilterButtons()
    {
        var defaultBg = Color.FromArgb("#F3F4F6");
        var defaultText = Color.FromArgb("#374151");

        FilterAllBtn.BackgroundColor = defaultBg;
        FilterAllBtn.TextColor = defaultText;
        FilterXomChieuBtn.BackgroundColor = defaultBg;
        FilterXomChieuBtn.TextColor = defaultText;
        FilterVinhHoiBtn.BackgroundColor = defaultBg;
        FilterVinhHoiBtn.TextColor = defaultText;
        FilterKhanhHoiBtn.BackgroundColor = defaultBg;
        FilterKhanhHoiBtn.TextColor = defaultText;
    }

    private void OnSortChanged(object? sender, EventArgs e)
    {
        _currentSort = SortPicker.SelectedIndex switch
        {
            1 => "name",
            2 => "distance",
            3 => "popular",
            _ => "default"
        };
        ApplyFilter();
    }

    private async void OnPoiSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not PoiDisplayItem poi)
            return;

        PoiCollectionView.SelectedItem = null;
        await NavigateToDetail(poi);
    }

    private async void OnPoiPlayClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is PoiDisplayItem poi)
        {
            await PlayAudio(poi.Code);
        }
    }

    private async Task NavigateToDetail(PoiDisplayItem poi)
    {
        await Shell.Current.GoToAsync(
            $"poiDetail" +
            $"?poiId={poi.Id}" +
            $"&name={Uri.EscapeDataString(poi.Name)}" +
            $"&desc={Uri.EscapeDataString(poi.Description)}" +
            $"&district={Uri.EscapeDataString(poi.District)}" +
            $"&code={Uri.EscapeDataString(poi.Code)}" +
            $"&lat={poi.Latitude}" +
            $"&lng={poi.Longitude}" +
            $"&trigger=List" +
            $"&source=PoiListPage");
    }

    private async Task PlayAudio(string poiCode)
    {
        // Resolve user if not already done
        if (string.IsNullOrEmpty(_userId) || _userId.StartsWith("ANON_"))
        {
            await ResolveUserAsync();
        }

        // If still empty, use anonymous
        if (string.IsNullOrEmpty(_userId))
        {
            _userId = _anonymousRef;
        }

        await Shell.Current.GoToAsync($"audio?qr={Uri.EscapeDataString(poiCode)}&userId={_userId}");
    }

    private string CalculateDistance(double lat, double lng)
    {
        var distance = CalculateDistanceMeters(lat, lng);
        if (distance == double.MaxValue) return "?";
        return distance < 1000 ? $"{distance:F0}m" : $"{distance / 1000:F1}km";
    }

    private double CalculateDistanceMeters(double lat, double lng)
    {
        if (lat == 0 && lng == 0) return double.MaxValue;

        const double R = 6371000;
        var dLat = ToRad(_currentLat - lat);
        var dLng = ToRad(_currentLng - lng);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat)) * Math.Cos(ToRad(_currentLat)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;

    private static readonly Dictionary<string, (double Lat, double Lng)> _knownPois = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Quán Bánh Mì Xóm Chiếu"] = (10.7530, 106.6878),
        ["Chợ Xóm Chiếu"] = (10.7540, 106.6880),
        ["Đình Xóm Chiếu"] = (10.7550, 106.6882),
        ["Nhà Cổ Xóm Chiếu"] = (10.7524, 106.6869),
        ["Nhà Thờ Vĩnh Hội"] = (10.7600, 106.6900),
        ["Khách Sạn Vĩnh Hội"] = (10.7610, 106.6910),
        ["Cây Cổ Thụ Vĩnh Hội"] = (10.7620, 106.6920),
        ["Hẻm Ăn Vĩnh Hội"] = (10.7617, 106.6896),
        ["Công Viên Bờ Kênh"] = (10.7632, 106.6931),
        ["Tiệm Cơm Khánh Hội"] = (10.7670, 106.6950),
        ["Chùa Khánh Hội"] = (10.7680, 106.6960),
        ["Hồ Cá Khánh Hội"] = (10.7690, 106.6970),
        ["Bến Tàu Khánh Hội"] = (10.7661, 106.6984),
        ["Chợ Đêm Khánh Hội"] = (10.7702, 106.6968),
    };

    private static double GetDefaultLatitude(string name)
        => _knownPois.TryGetValue(name, out var c) ? c.Lat : 10.7553;

    private static double GetDefaultLongitude(string name)
        => _knownPois.TryGetValue(name, out var c) ? c.Lng : 106.6602;
}

public class PoiDisplayItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string DistanceText { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
    public int ListenCount { get; set; }
}
