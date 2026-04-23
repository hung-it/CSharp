using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

public partial class TourManagerPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private string _userId = string.Empty;
    private string _anonymousRef = $"ANON_{Guid.NewGuid():N}";

    public TourManagerPage()
    {
        InitializeComponent();
        _httpClient = AppConfig.CreateHttpClient();
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ResolveUserAsync();
        await LoadToursAsync();
        await LoadMyStatsAsync();
    }

    private async Task ResolveUserAsync()
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("users/resolve", new
            {
                Username = AppConfig.DefaultExternalRef,  // Changed from ExternalRef to Username
                PreferredLanguage = AppConfig.DefaultPreferredLanguage
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UserResolveResult>();
                if (result?.Success == true)
                {
                    _userId = result.Id.ToString();
                }
            }
        }
        catch
        {
            _userId = _anonymousRef;
        }
    }

    private async Task LoadToursAsync()
    {
        try
        {
            var tours = await _httpClient.GetFromJsonAsync<List<TourData>>("tours");

            if (tours == null || tours.Count == 0)
            {
                // Load default tours
                tours = GetDefaultTours();
            }

            var displayTours = tours.Select(t => new TourDisplayItem
            {
                Id = t.Id,
                Name = t.Name ?? "Tour không tên",
                Description = t.Description ?? "Không có mô tả",
                StopCount = "10", // Will be updated from API
                Duration = "1-2h",
                Difficulty = "Dễ",
                Code = t.Code ?? ""
            }).ToList();

            TourCollectionView.ItemsSource = displayTours;
        }
        catch
        {
            // Load default tours on error
            TourCollectionView.ItemsSource = GetDefaultTourDisplay();
        }
    }

    private async Task LoadMyStatsAsync()
    {
        if (string.IsNullOrEmpty(_userId) || !Guid.TryParse(_userId, out var userGuid))
            return;

        try
        {
            var sessions = await _httpClient.GetFromJsonAsync<List<SessionData>>($"sessions/users/{userGuid}");
            
            if (sessions != null)
            {
                var uniqueTours = sessions.Select(s => s.PoiId).Distinct().Count();
                var audioCount = sessions.Count;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MyTourCount.Text = Math.Min(uniqueTours, 99).ToString();
                    MyPoiCount.Text = Math.Min(uniqueTours, 99).ToString();
                    MyAudioCount.Text = Math.Min(audioCount, 999).ToString();
                });
            }
        }
        catch { /* Silent */ }
    }

    private List<TourData> GetDefaultTours()
    {
        return new List<TourData>
        {
            // Tour 1: Khám Phá Phố Ăn Vĩnh Khách (tất cả 12 POIs)
            new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Code = "TOUR001", Name = "Khám Phá Phố Ăn Vĩnh Khách", Description = "Hành trình khám phá toàn bộ 12 điểm ẩm thực và di sản văn hóa đặc sắc tại Phố Cổ Vĩnh Khách. Phù hợp cho du khách muốn trải nghiệm trọn vẹn văn hóa ẩm thực Sài Gòn." },
            // Tour 2: Tuyến Ăn Uống Đường Phố (6 POIs)
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Code = "TOUR002", Name = "Tuyến Ăn Uống Đường Phố", Description = "Tập trung vào các điểm ăn uống đặc sản: bánh mì, cơm, hẻm ăn và chợ đêm. Phù hợp cho ai yêu thích ẩm thực đường phố Việt Nam." },
            // Tour 3: Khám Phá Di Sản Văn Hóa (6 POIs)
            new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Code = "TOUR003", Name = "Hành Trình Di Sản Văn Hóa", Description = "Khám phá các điểm di sản lịch sử: đình, chùa, nhà thờ, nhà cổ. Phù hợp cho du khách yêu thích lịch sử và kiến trúc." }
        };
    }

    private List<TourDisplayItem> GetDefaultTourDisplay()
    {
        // Use fixed GUIDs to match database seeder
        return new List<TourDisplayItem>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "🗺️ Khám Phá Phố Ăn Vĩnh Khách",
                Description = "Hành trình khám phá toàn bộ 12 điểm ẩm thực và di sản văn hóa đặc sắc tại Phố Cổ Vĩnh Khách",
                StopCount = "12",
                Duration = "2-3h",
                Difficulty = "Trung bình",
                Code = "TOUR001"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "🍜 Tuyến Ăn Uống Đường Phố",
                Description = "Tập trung vào các điểm ăn uống đặc sản: bánh mì, cơm, hẻm ăn và chợ đêm",
                StopCount = "6",
                Duration = "1-2h",
                Difficulty = "Dễ",
                Code = "TOUR002"
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "🏛️ Hành Trình Di Sản Văn Hóa",
                Description = "Khám phá các điểm di sản lịch sử: đình, chùa, nhà thờ, nhà cổ",
                StopCount = "6",
                Duration = "1-2h",
                Difficulty = "Dễ",
                Code = "TOUR003"
            }
        };
    }

    private async void OnTourSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TourDisplayItem tour)
            return;

        TourCollectionView.SelectedItem = null;

        await Shell.Current.GoToAsync(
            $"tourDetail" +
            $"?tourId={tour.Id}" +
            $"&name={Uri.EscapeDataString(tour.Name)}" +
            $"&desc={Uri.EscapeDataString(tour.Description)}");
    }

    private async void OnStartTourClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is TourDisplayItem tour)
        {
            // Start tour view tracking
            if (!string.IsNullOrEmpty(_userId) && Guid.TryParse(_userId, out var userId))
            {
                try
                {
                    await _httpClient.PostAsJsonAsync($"tours/{tour.Id}/view/start", new
                    {
                        UserId = userId,
                        AnonymousRef = _anonymousRef
                    });
                }
                catch { /* Silent */ }
            }

            // Navigate to tour detail
            await Shell.Current.GoToAsync(
                $"tourDetail" +
                $"?tourId={tour.Id}" +
                $"&name={Uri.EscapeDataString(tour.Name)}" +
                $"&desc={Uri.EscapeDataString(tour.Description)}");
        }
    }
}

// Data Models
public class TourData
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class TourDisplayItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StopCount { get; set; } = "0";
    public string Duration { get; set; } = "0h";
    public string Difficulty { get; set; } = "-";
    public string Code { get; set; } = string.Empty;
}

public class SessionData
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PoiId { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public int? DurationSeconds { get; set; }
}
