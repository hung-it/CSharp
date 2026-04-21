using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private string _resolvedUserId = string.Empty;

    public MainPage()
    {
        InitializeComponent();
        _httpClient = AppConfig.CreateHttpClient();
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        await ResolveUserAsync();
        await LoadDataAsync();
    }

    // Resolve userId thật từ API thay vì hardcode
    private async Task ResolveUserAsync()
    {
        _resolvedUserId = await AppConfig.ResolveDefaultUserIdAsync(_httpClient);
    }

    private async Task LoadDataAsync()
    {
        // Chạy song song 4 request
        var poisTask    = _httpClient.GetFromJsonAsync<List<object>>("pois");
        var toursTask   = _httpClient.GetFromJsonAsync<List<object>>("tours");
        var topTask     = _httpClient.GetFromJsonAsync<List<TopPoiItem>>("analytics/top?limit=5");
        var usageTask   = _httpClient.GetFromJsonAsync<UsageStats>("analytics/usage?days=7");

        try { PoiCountLabel.Text   = ((await poisTask)?.Count  ?? 0).ToString(); } catch { PoiCountLabel.Text   = "—"; }
        try { TourCountLabel.Text  = ((await toursTask)?.Count ?? 0).ToString(); } catch { TourCountLabel.Text  = "—"; }

        try
        {
            var top = await topTask ?? new List<TopPoiItem>();
            if (top.Count == 0)
            {
                TopPoisEmptyLabel.IsVisible = true;
            }
            else
            {
                TopPoisEmptyLabel.IsVisible = false;
                // Gán rank hiển thị
                for (int i = 0; i < top.Count; i++)
                    top[i].Rank = i switch { 0 => "🥇", 1 => "🥈", 2 => "🥉", _ => $"{i + 1}." };

                TopPoisCollectionView.ItemsSource = top;
                ListenCountLabel.Text = top.Sum(x => x.ListenCount).ToString();
            }
        }
        catch
        {
            TopPoisEmptyLabel.IsVisible = true;
            ListenCountLabel.Text = "—";
        }

        try
        {
            var usage = await usageTask;
            WeekListenLabel.Text      = (usage?.TotalListens ?? 0).ToString();
            WeekActiveCellsLabel.Text = (usage?.ActiveCells ?? 0).ToString();
        }
        catch
        {
            WeekListenLabel.Text      = "—";
            WeekActiveCellsLabel.Text = "—";
        }
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        // Reset về —
        PoiCountLabel.Text = TourCountLabel.Text = ListenCountLabel.Text = "…";
        WeekListenLabel.Text = WeekActiveCellsLabel.Text = "…";
        await LoadDataAsync();
    }

    private async void OnScanQRClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("qrScan");
    }

    private async void OnOpenAdminWebClicked(object? sender, EventArgs e)
    {
        try
        {
            await Launcher.OpenAsync(new Uri(AppConfig.FrontendBaseUrl));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Lỗi", $"Không thể mở web admin: {ex.Message}", "OK");
        }
    }
}

public class TopPoiItem
{
    public string PoiName { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public int ListenCount { get; set; }
    // Không từ API, gán trong code
    public string Rank { get; set; } = string.Empty;
}

public class UsageStats
{
    public int Days { get; set; }
    public int TotalListens { get; set; }
    public int ActiveCells { get; set; }
}
