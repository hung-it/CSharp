using System.Net.Http.Json;
using Microsoft.Maui.Devices.Sensors;

namespace VinhKhanhAudioGuide.App;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private string _userId = string.Empty;
    private string _anonymousRef = string.Empty;
    private string _currentLanguage = "vi";
    private List<NearbyPoiItem> _nearbyPois = new();
    private double _currentLat = 10.7553;
    private double _currentLng = 106.6602;

    public MainPage()
    {
        InitializeComponent();
        _httpClient = AppConfig.CreateHttpClient();
        _anonymousRef = $"ANON_{Guid.NewGuid():N}";

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            await ResolveUserAsync();

            await Task.WhenAll(
                LoadStatsAsync(),
                LoadNearbyPoisAsync(),
                LoadPopularPoisAsync(),
                LoadMyStatsAsync()
            );
        }
        catch (Exception ex)
        {
            ConnectionStatusLabel.Text = "⚠️ Chế độ offline";
            ConnectionStatusLabel.TextColor = Colors.Orange;
            System.Diagnostics.Debug.WriteLine($"Init error: {ex.Message}");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async Task ResolveUserAsync()
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("users/resolve", new
            {
                Username = "demo",
                PreferredLanguage = _currentLanguage,
                Password = "1"
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                if (result?.Success == true)
                {
                    _userId = result.Id.ToString();
                    if (AppConfig.IsLoggedIn)
                    {
                        ConnectionStatusLabel.Text = "✅ Đã đăng nhập";
                    }
                    else
                    {
                        ConnectionStatusLabel.Text = "🔑 Đăng nhập nhanh (demo)";
                    }
                    LastSyncLabel.Text = DateTime.Now.ToString("HH:mm");
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

    private async Task LoadStatsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Stats loaded");
        }
        catch { }
    }

    private async Task LoadMyStatsAsync()
    {
        if (!AppConfig.IsLoggedIn)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MyPoiVisitCount.Text = "-";
                MyListenCount.Text = "-";
                MyListenTime.Text = "-";
            });
            return;
        }

        var userId = AppConfig.CurrentUserId;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            return;

        try
        {
            var sessions = await _httpClient.GetFromJsonAsync<List<MySessionInfo>>(
                $"sessions/users/{userGuid}");

            if (sessions != null)
            {
                var totalListens = sessions.Count;
                var totalSeconds = sessions
                    .Where(s => s.DurationSeconds.HasValue)
                    .Sum(s => s.DurationSeconds!.Value);
                var uniquePois = sessions.Select(s => s.PoiId).Distinct().Count();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MyPoiVisitCount.Text = uniquePois.ToString();
                    MyListenCount.Text = totalListens.ToString();
                    MyListenTime.Text = Math.Round(totalSeconds / 60.0).ToString();
                });
            }
        }
        catch { }
    }

    private async Task LoadNearbyPoisAsync()
    {
        try
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status == PermissionStatus.Granted)
                {
                    var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(
                        GeolocationAccuracy.High,
                        TimeSpan.FromSeconds(10)));

                    if (location != null)
                    {
                        _currentLat = location.Latitude;
                        _currentLng = location.Longitude;
                    }
                    else
                    {
                        var lastLocation = await Geolocation.Default.GetLastKnownLocationAsync();
                        if (lastLocation != null)
                        {
                            _currentLat = lastLocation.Latitude;
                            _currentLng = lastLocation.Longitude;
                        }
                    }
                }
            }
            catch { }

            var pois = await _httpClient.GetFromJsonAsync<List<PoiData>>("pois");

            if (pois != null && pois.Count > 0)
            {
                _nearbyPois = pois.Select(p => new NearbyPoiItem
                {
                    Id = p.Id,
                    Code = p.Code ?? "",
                    Name = p.Name ?? "Không tên",
                    DistanceText = CalculateDistance(p.Latitude, p.Longitude)
                })
                .OrderBy(p => p.DistanceMeters)
                .Take(5)
                .ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    NearbyPoisCollectionView.ItemsSource = _nearbyPois;
                    NoNearbyLabel.IsVisible = _nearbyPois.Count == 0;
                });
            }
        }
        catch
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NoNearbyLabel.Text = "⚠️ Bật GPS để xem POI gần bạn";
            });
        }
    }

    private string CalculateDistance(double lat, double lng)
    {
        var distance = GetDistanceMeters(lat, lng);
        if (distance == double.MaxValue) return "?";

        return distance < 1000
            ? $"{distance:F0}m"
            : $"{distance / 1000:F1}km";
    }

    private double GetDistanceMeters(double lat, double lng)
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

    private async Task LoadPopularPoisAsync()
    {
        try
        {
            var topPois = await _httpClient.GetFromJsonAsync<List<TopPoiItem>>("analytics/top?limit=5");

            if (topPois != null && topPois.Count > 0)
            {
                for (int i = 0; i < topPois.Count; i++)
                {
                    topPois[i].Rank = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => $"{i + 1}." };
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PopularPoisCollectionView.ItemsSource = topPois;
                });
            }
        }
        catch { }
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

    private async void OnMapClicked(object? sender, EventArgs e)
    {
        await NavigateSafeAsync("//map");
    }

    private async void OnScanQRClicked(object? sender, EventArgs e)
    {
        await NavigateSafeAsync("qrScan");
    }

    private async void OnToursClicked(object? sender, EventArgs e)
    {
        await NavigateSafeAsync("//tour");
    }

    private async void OnPoiListClicked(object? sender, EventArgs e)
    {
        await NavigateSafeAsync("//poi");
    }

    private async void OnAudioListClicked(object? sender, EventArgs e)
    {
        await NavigateSafeAsync("//poi");
    }

    private async void OnNearbyPlayClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is NearbyPoiItem poi)
        {
            await PlayPoiAudioAsync(poi.Code);
        }
    }

    private async Task PlayPoiAudioAsync(string poiCode)
    {
        if (string.IsNullOrEmpty(_userId) || _userId.StartsWith("ANON_"))
        {
            await ResolveUserAsync();
        }

        if (string.IsNullOrEmpty(_userId))
        {
            _userId = _anonymousRef;
        }

        await Shell.Current.GoToAsync($"audio?qr={Uri.EscapeDataString(poiCode)}&userId={_userId}");
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        RefreshBtn.IsEnabled = false;
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        LastSyncLabel.Text = "⏳ đang cập nhật...";

        try
        {
            await InitializeAsync();
            LastSyncLabel.Text = DateTime.Now.ToString("HH:mm");
        }
        finally
        {
            RefreshBtn.IsEnabled = true;
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnLanguageViClicked(object? sender, EventArgs e)
    {
        _currentLanguage = "vi";
        LangViBtn.BackgroundColor = Color.FromArgb("#EC4899");
        LangViBtn.TextColor = Colors.White;
        LangEnBtn.BackgroundColor = Color.FromArgb("#E5E7EB");
        LangEnBtn.TextColor = Color.FromArgb("#374151");

        await DisplayAlertAsync("🌐 Ngôn ngữ", "✅ Đã chuyển sang Tiếng Việt", "OK");
    }

    private async void OnLanguageEnClicked(object? sender, EventArgs e)
    {
        if (!FeatureGate.IsPremium)
        {
            await DisplayAlertAsync("💎 Premium",
                "🔒 Audio tiếng Anh là tính năng Premium.\n\n" +
                "Vui lòng nâng cấp tài khoản để sử dụng.", "OK");
            return;
        }

        _currentLanguage = "en";
        LangEnBtn.BackgroundColor = Color.FromArgb("#EC4899");
        LangEnBtn.TextColor = Colors.White;
        LangViBtn.BackgroundColor = Color.FromArgb("#E5E7EB");
        LangViBtn.TextColor = Color.FromArgb("#374151");

        await DisplayAlertAsync("🌐 Ngôn ngữ", "✅ Switched to English 🇬🇧", "OK");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMyStatsAsync();
    }
}

public class UserResolveResult
{
    public bool Success { get; set; }
    public Guid Id { get; set; }
    public string? Username { get; set; }
}

public class NearbyPoiItem
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DistanceText { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
}

public class MySessionInfo
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PoiId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public int? DurationSeconds { get; set; }
}

public class TopPoiItem
{
    public string? poiName { get; set; }
    public string? district { get; set; }
    public int listeningCount { get; set; }
    public string PoiName => poiName ?? string.Empty;
    public string District => district ?? string.Empty;
    public int ListenCount => listeningCount;
    public string Rank { get; set; } = string.Empty;
}
