using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

public partial class MapPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private List<PoiData> _allPois = new();
    private string _userId = string.Empty;
    private double _currentLat = 10.7553;
    private double _currentLng = 106.6602;
    private bool _isTracking = false;
    private string _anonymousRef = $"ANON_{Guid.NewGuid():N}";

    // Geofence tracking - prevent repeated triggers for same POI in session
    private readonly HashSet<string> _visitedPoiCodes = new();

    // Auto-play state
    private bool AutoPlayEnabled => Preferences.Default.Get("autoPlayEnabled", false);
    private double GpsSensitivityMeters => Preferences.Default.Get("gpsSensitivity", 50);

    public MapPage()
    {
        InitializeComponent();
        _httpClient = AppConfig.CreateHttpClient();

        mapWebView.Navigated += MapWebView_Navigated;
        _ = InitializeAsync();
    }

    private async void MapWebView_Navigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result == WebNavigationResult.Success)
        {
            await UpdateMapMarkersAsync();
        }
    }

    private async Task InitializeAsync()
    {
        await ResolveUserAsync();
        await LoadPoisAsync();
        await StartLocationTrackingAsync();
        LoadMapHtml();
    }

    private void LoadMapHtml()
    {
        var html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        html, body, #map { width: 100%; height: 100%; }
        .poi-popup { font-family: -apple-system, BlinkMacSystemFont, sans-serif; }
        .poi-popup h4 { margin: 0 0 4px; color: #1F2937; }
        .poi-popup p { margin: 0; font-size: 12px; color: #6B7280; }
        .user-marker {
            background: white;
            border: 3px solid #3B82F6;
            border-radius: 50%;
            width: 18px !important;
            height: 18px !important;
            margin-left: -9px !important;
            margin-top: -9px !important;
            box-shadow: 0 2px 6px rgba(0,0,0,0.3);
        }
        .user-marker-inner {
            background: #3B82F6;
            border-radius: 50%;
            width: 8px;
            height: 8px;
            margin: 3px auto;
        }
        .user-pulse {
            background: rgba(59, 130, 246, 0.2);
            border-radius: 50%;
            width: 36px;
            height: 36px;
            margin-left: -9px;
            margin-top: -9px;
            animation: pulse 2s infinite;
        }
        @keyframes pulse {
            0% { transform: scale(0.8); opacity: 1; }
            100% { transform: scale(2); opacity: 0; }
        }
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        var map = L.map('map').setView([10.7553, 106.6602], 15);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(map);
        window.map = map;
        window.userMarker = null;
        window.userPulse = null;
    </script>
</body>
</html>";
        mapWebView.Source = new HtmlWebViewSource { Html = html };
    }

    private async Task UpdateUserLocationMarkerAsync()
    {
        if (mapWebView == null) return;
        try
        {
            var js = $@"
(function() {{
    var lat = {_currentLat};
    var lng = {_currentLng};

    if (window.userMarker) {{
        window.userMarker.setLatLng([lat, lng]);
    }} else {{
        // Pulse circle
        var pulseIcon = L.divIcon({{
            className: 'user-pulse-container',
            html: '<div class=""user-pulse""></div>',
            iconSize: [36, 36],
            iconAnchor: [18, 18]
        }});
        window.userPulse = L.marker([lat, lng], {{ icon: pulseIcon }}).addTo(window.map);

        // Blue dot marker
        var userIcon = L.divIcon({{
            className: 'user-marker',
            html: '<div class=""user-marker-inner""></div>',
            iconSize: [18, 18],
            iconAnchor: [9, 9]
        }});
        window.userMarker = L.marker([lat, lng], {{
            icon: userIcon,
            zIndexOffset: 1000
        }}).addTo(window.map);
    }}
}})();
";
            await mapWebView.EvaluateJavaScriptAsync(js);
        }
        catch { }
    }

    private async Task UpdateMapMarkersAsync()
    {
        if (_allPois.Count == 0) return;

        var markersScript = string.Join("\n", _allPois.Select(poi =>
        {
            var lat = poi.Latitude != 0 ? poi.Latitude : GetDefaultLatitude(poi.Name ?? "");
            var lng = poi.Longitude != 0 ? poi.Longitude : GetDefaultLongitude(poi.Name ?? "");
            var escapedName = (poi.Name ?? "").Replace("'", "\\'");
            var escapedDesc = (poi.Description ?? "").Replace("'", "\\'");
            return $@"
L.marker([{lat}, {lng}])
    .addTo(window.map)
    .bindPopup('<div class=""poi-popup""><h4>{escapedName}</h4><p>{escapedDesc}</p></div>');";
        }));

        var js = $@"
var bounds = L.latLngBounds([]);
{markersScript}
window.map.fitBounds(bounds, {{ padding: [50, 50] }});
";
        await mapWebView.EvaluateJavaScriptAsync(js);
    }

    private async Task ResolveUserAsync()
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("users/resolve", new
            {
                Username = "demo",
                PreferredLanguage = AppConfig.DefaultPreferredLanguage,
                Password = "1"
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                if (result?.Success == true)
                {
                    _userId = result.Id.ToString();
                    System.Diagnostics.Debug.WriteLine($"User resolved: {_userId}");
                }
                else
                {
                    _userId = _anonymousRef;
                }
            }
            else
            {
                _userId = _anonymousRef;
            }
        }
        catch
        {
            _userId = _anonymousRef;
        }
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

            _allPois = pois;
            UpdateNearbyList();
        }
        catch
        {
            _allPois = GetKnownPois();
            UpdateNearbyList();
        }
    }

    private List<PoiData> GetKnownPois()
    {
        return new List<PoiData>
        {
            new() { Id = Guid.NewGuid(), Code = "POI001", Name = "Quán Bánh Mì Đặc Biệt", Description = "Quán bánh mì nổi tiếng nhất vùng", District = "Xóm Chiếu", Latitude = 10.7530, Longitude = 106.6878 },
            new() { Id = Guid.NewGuid(), Code = "POI005", Name = "Chợ Xóm Chiếu", Description = "Chợ truyền thống với nhiều đặc sản", District = "Xóm Chiếu", Latitude = 10.7540, Longitude = 106.6880 },
            new() { Id = Guid.NewGuid(), Code = "POI009", Name = "Đình Xóm Chiếu", Description = "Đình thờ cổ có lịch sử hơn 100 năm", District = "Xóm Chiếu", Latitude = 10.7550, Longitude = 106.6882 },
            new() { Id = Guid.NewGuid(), Code = "POI003", Name = "Hẻm Ăn Vĩnh Hội", Description = "Khu hẻm ăn uống địa phương nổi tiếng", District = "Vĩnh Hội", Latitude = 10.7617, Longitude = 106.6896 },
            new() { Id = Guid.NewGuid(), Code = "POI006", Name = "Nhà Thờ Vĩnh Hội", Description = "Nhà thờ Công giáo với kiến trúc Pháp", District = "Vĩnh Hội", Latitude = 10.7600, Longitude = 106.6900 },
            new() { Id = Guid.NewGuid(), Code = "POI002", Name = "Tiệm Cơm Gia Đình", Description = "Tiệm cơm bình dân được yêu thích", District = "Khánh Hội", Latitude = 10.7670, Longitude = 106.6950 },
            new() { Id = Guid.NewGuid(), Code = "POI010", Name = "Chùa An Lạc", Description = "Chùa Phật giáo với kiến trúc đẹp", District = "Khánh Hội", Latitude = 10.7680, Longitude = 106.6960 }
        };
    }

    private void UpdateNearbyList()
    {
        NearbyPoiStack.Children.Clear();

        var nearbyPois = _allPois
            .Select(p => new
            {
                Poi = p,
                Distance = CalculateDistanceMeters(
                    p.Latitude != 0 ? p.Latitude : GetDefaultLatitude(p.Name ?? ""),
                    p.Longitude != 0 ? p.Longitude : GetDefaultLongitude(p.Name ?? ""))
            })
            .OrderBy(x => x.Distance)
            .Take(5)
            .ToList();

        foreach (var item in nearbyPois)
        {
            var distanceText = item.Distance < 1000
                ? $"{item.Distance:F0}m"
                : $"{item.Distance / 1000:F1}km";

            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#F0F9FF"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Padding = 12,
                WidthRequest = 160
            };

            var stack = new VerticalStackLayout { Spacing = 4 };
            var poiName = item.Poi.Name ?? "";
            stack.Children.Add(new Label
            {
                Text = poiName.Length > 20 ? poiName.Substring(0, 20) + "..." : poiName,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1E40AF")
            });
            stack.Children.Add(new Label
            {
                Text = $"{item.Poi.District} • {distanceText}",
                FontSize = 10,
                TextColor = Color.FromArgb("#3B82F6")
            });

            var playBtn = new Button
            {
                Text = "▶",
                BackgroundColor = Color.FromArgb("#3B82F6"),
                TextColor = Colors.White,
                CornerRadius = 12,
                HeightRequest = 32,
                FontSize = 14,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var poiCode = item.Poi.Code ?? "";
            if (string.IsNullOrEmpty(poiCode)) continue;
            playBtn.Clicked += async (s, e) => await PlayPoiAudio(poiCode);

            stack.Children.Add(playBtn);
            card.Content = stack;
            NearbyPoiStack.Children.Add(card);
        }
    }

    private async Task StartLocationTrackingAsync()
    {
        if (_isTracking) return;
        _isTracking = true;

        try
        {
            // Request location permission
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            
            if (status != PermissionStatus.Granted)
        {
            LocationStatusLabel.Text = "⚠️ Cần cấp quyền GPS";
            return;
        }

        var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(
            GeolocationAccuracy.High,
            TimeSpan.FromSeconds(10)));

        if (location != null)
        {
            _currentLat = location.Latitude;
            _currentLng = location.Longitude;
            LocationStatusLabel.Text = $"📍 {_currentLat:F4}, {_currentLng:F4}";
            UpdateNearbyList();
            await LogRoutePointAsync();
            await CheckGeofenceAsync();
            await UpdateMapCenterAsync();
            await UpdateUserLocationMarkerAsync();
        }
        else
        {
            var lastLocation = await Geolocation.Default.GetLastKnownLocationAsync();
            if (lastLocation != null)
            {
                _currentLat = lastLocation.Latitude;
                _currentLng = lastLocation.Longitude;
                LocationStatusLabel.Text = $"📍 {_currentLat:F4}, {_currentLng:F4} (cache)";
                UpdateNearbyList();
                await UpdateMapCenterAsync();
            }
            else
            {
                LocationStatusLabel.Text = "❌ Không tìm được GPS";
            }
        }
    }
    catch (FeatureNotSupportedException ex)
    {
        LocationStatusLabel.Text = "❌ GPS không hỗ trợ: " + ex.Message;
            System.Diagnostics.Debug.WriteLine($"Location error: {ex.Message}");
        }
        catch (Exception ex)
        {
            LocationStatusLabel.Text = "⚠️ GPS không khả dụng";
            System.Diagnostics.Debug.WriteLine($"Location error: {ex.Message}");
        }
        finally
        {
            _isTracking = false;
        }
    }

    private async Task UpdateMyLocationAsync()
    {
        // Request location permission first
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlertAsync("🔑 Quyền GPS", "Vui lòng cấp quyền truy cập vị trí để sử dụng tính năng này.", "OK");
            return;
        }

        try
        {
            LocationStatusLabel.Text = "⏳ Đang định vị...";

            var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(
                GeolocationAccuracy.High,
                TimeSpan.FromSeconds(10)));

            if (location != null)
            {
                _currentLat = location.Latitude;
                _currentLng = location.Longitude;
                LocationStatusLabel.Text = $"📍 {_currentLat:F4}, {_currentLng:F4}";
                UpdateNearbyList();
                await UpdateMapCenterAsync();
                await LogRoutePointAsync();
                await CheckGeofenceAsync();
                await UpdateUserLocationMarkerAsync();
            }
            else
            {
                LocationStatusLabel.Text = "❌ Không lấy được vị trí";
            }
        }
        catch (FeatureNotSupportedException ex)
        {
            await DisplayAlertAsync("❌ Lỗi GPS", $"GPS không hỗ trợ: {ex.Message}", "OK");
            LocationStatusLabel.Text = "❌ GPS không hỗ trợ";
        }
        catch (Exception)
        {
            await DisplayAlertAsync("❌ Lỗi GPS", "Không thể lấy vị trí của bạn.", "OK");
            LocationStatusLabel.Text = "❌ Lỗi GPS";
        }
    }

    private async Task UpdateMapCenterAsync()
    {
        var js = $"window.map.setView([{_currentLat}, {_currentLng}], 15);";
        try { await mapWebView.EvaluateJavaScriptAsync(js); } catch { }
    }

    private async Task LogRoutePointAsync()
    {
        if (string.IsNullOrEmpty(_userId)) return;
        try
        {
            await _httpClient.PostAsJsonAsync($"routes/anonymous/{_anonymousRef}/points", new
            {
                Latitude = _currentLat,
                Longitude = _currentLng,
                Source = "gps"
            });
        }
        catch { }
    }

    private async Task CheckGeofenceAsync()
    {
        // Check if auto-play is enabled (Premium feature)
        if (!FeatureGate.IsPremium)
            return;

        var triggerRadius = GpsSensitivityMeters;

        foreach (var poi in _allPois)
        {
            var lat = poi.Latitude != 0 ? poi.Latitude : GetDefaultLatitude(poi.Name ?? "");
            var lng = poi.Longitude != 0 ? poi.Longitude : GetDefaultLongitude(poi.Name ?? "");
            var distance = CalculateDistanceMeters(lat, lng);

            if (distance <= triggerRadius)
            {
                await RecordGeofenceEventAsync(poi.Id, "enter", distance);

                // Auto-play only once per POI per session
                if (!_visitedPoiCodes.Contains(poi.Code ?? ""))
                {
                    _visitedPoiCodes.Add(poi.Code ?? "");
                    await TriggerAutoPlayAsync(poi.Code ?? "", poi.Name ?? "");
                }
            }
        }
    }

    private async Task TriggerAutoPlayAsync(string poiCode, string poiName)
    {
        if (string.IsNullOrEmpty(poiCode))
            return;

        System.Diagnostics.Debug.WriteLine($"[Geofence] Auto-playing: {poiName} ({poiCode})");

        // Resolve user if needed
        if (string.IsNullOrEmpty(_userId) || _userId.StartsWith("ANON_"))
        {
            try { await ResolveUserAsync(); } catch { }
        }

        var finalUserId = string.IsNullOrEmpty(_userId) || _userId.StartsWith("ANON_")
            ? _anonymousRef
            : _userId;

        try
        {
            // Navigate to audio page
            await Shell.Current.GoToAsync($"audio?qr={Uri.EscapeDataString(poiCode)}&userId={Uri.EscapeDataString(finalUserId)}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Geofence] Auto-play navigation error: {ex.Message}");
        }
    }

    private async Task RecordGeofenceEventAsync(Guid poiId, string eventType, double distance)
    {
        if (string.IsNullOrEmpty(_userId) || !Guid.TryParse(_userId, out var userId)) return;
        try
        {
            await _httpClient.PostAsJsonAsync("geofence/events", new
            {
                UserId = userId,
                PoiId = poiId,
                EventType = eventType,
                Latitude = _currentLat,
                Longitude = _currentLng,
                DistanceFromCenterMeters = distance,
                AnonymousRef = _anonymousRef
            });
        }
        catch { }
    }

    private void OnCenterLocationClicked(object? sender, EventArgs e)
    {
        _ = UpdateMyLocationAsync();
    }

    private async Task NavigateSafeAsync(string route)
    {
        try
        {
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error ({route}): {ex.Message}");
            await DisplayAlertAsync("❌ Lỗi điều hướng", "Không thể mở màn hình này. Vui lòng thử lại.", "OK");
        }
    }

    private async void OnRefreshMapClicked(object? sender, EventArgs e)
    {
        await LoadPoisAsync();
        await StartLocationTrackingAsync();
    }

    private async void OnScanQRClicked(object? sender, EventArgs e)
    {
        await NavigateSafeAsync("qrScan");
    }

    private async void OnShowAllPoiClicked(object? sender, EventArgs e)
    {
        await NavigateSafeAsync("//poi");
    }

    private async Task PlayPoiAudio(string poiCode)
    {
        // If userId is empty or anonymous, try to resolve first
        if (string.IsNullOrEmpty(_userId) || _userId.StartsWith("ANON_"))
        {
            try
            {
                await ResolveUserAsync();
            }
            catch
            {
                // Ignore resolution errors, will use anonymous
            }
        }

        // Use anonymous ref if still unresolved
        var finalUserId = string.IsNullOrEmpty(_userId) || _userId.StartsWith("ANON_")
            ? _anonymousRef
            : _userId;

        try
        {
            await Shell.Current.GoToAsync($"audio?qr={Uri.EscapeDataString(poiCode)}&userId={Uri.EscapeDataString(finalUserId)}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            await DisplayAlertAsync("Lỗi", "Không thể mở trình phát audio. Vui lòng thử lại.", "OK");
        }
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
        ["Nhà Thờ Vĩnh Hội"] = (10.7600, 106.6900),
        ["Tiệm Cơm Khánh Hội"] = (10.7670, 106.6950),
        ["Chùa An Lạc"] = (10.7680, 106.6960),
    };

    private static double GetDefaultLatitude(string name)
        => _knownPois.TryGetValue(name, out var c) ? c.Lat : 10.7553;

    private static double GetDefaultLongitude(string name)
        => _knownPois.TryGetValue(name, out var c) ? c.Lng : 106.6602;
}
