using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

public partial class TourManagerPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public TourManagerPage()
    {
        InitializeComponent();
        _httpClient = new HttpClient { BaseAddress = new Uri(AppConfig.ApiBaseUrl) };
        LoadTours();
    }

    private async void LoadTours()
    {
        try
        {
            var tours = await _httpClient.GetFromJsonAsync<List<TourSummary>>("tours");
            TourCollectionView.ItemsSource = tours ?? new List<TourSummary>();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể tải danh sách tour: {ex.Message}", "OK");
        }
    }

    private async void OnTourSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TourSummary tour)
            return;

        TourCollectionView.SelectedItem = null;

        await Shell.Current.GoToAsync(
            $"tourDetail" +
            $"?tourId={tour.Id}" +
            $"&name={Uri.EscapeDataString(tour.Name ?? string.Empty)}" +
            $"&desc={Uri.EscapeDataString(tour.Description ?? string.Empty)}");
    }
}

public class TourSummary
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string StopCountText => "Nhấn để xem lộ trình →";
}
