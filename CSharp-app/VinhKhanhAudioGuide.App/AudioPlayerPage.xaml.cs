using CommunityToolkit.Maui.Views;
using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

[QueryProperty(nameof(QrPayload), "qr")]
[QueryProperty(nameof(UserId), "userId")]
public partial class AudioPlayerPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private string _currentLanguage = "vi";
    private string _qrPayload = string.Empty;
    private string _userId = string.Empty;
    private bool _isPlaying = false;
    private Guid _sessionId = Guid.Empty;
    private DateTime _playStartedAt = DateTime.MinValue;
    private int _totalPlayedSeconds = 0;
    private bool _sessionEnded = false;
    private bool _loadRequested = false;

    public string QrPayload
    {
        set
        {
            _qrPayload = Uri.UnescapeDataString(value ?? string.Empty);
            TryLoadAudio();
        }
    }

    public string UserId
    {
        set
        {
            _userId = value ?? string.Empty;
            TryLoadAudio();
        }
    }

    // Chỉ load khi cả 2 params đã có, và chỉ load 1 lần
    private void TryLoadAudio()
    {
        if (_loadRequested) return;
        if (string.IsNullOrEmpty(_qrPayload) || string.IsNullOrEmpty(_userId)) return;
        _loadRequested = true;
        MainThread.BeginInvokeOnMainThread(() => _ = LoadAudioAsync());
    }

    public AudioPlayerPage()
    {
        InitializeComponent();
        _httpClient = new HttpClient { BaseAddress = new Uri(AppConfig.ApiBaseUrl) };
    }

    private async Task LoadAudioAsync()
    {
        if (string.IsNullOrEmpty(_qrPayload) || string.IsNullOrEmpty(_userId))
            return;

        // Kết thúc session cũ nếu đang đổi ngôn ngữ
        await EndSessionAsync();

        StatusLabel.Text = "Đang tải...";
        PlayPauseButton.IsEnabled = false;
        _sessionEnded = false;
        _totalPlayedSeconds = 0;

        try
        {
            var response = await _httpClient.PostAsJsonAsync("qr/start", new
            {
                UserId = Guid.Parse(_userId),
                QrPayload = _qrPayload,
                LanguageCode = _currentLanguage
            });

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                StatusLabel.Text = "Lỗi: không thể tải audio";
                await DisplayAlert("Lỗi", err, "OK");
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<QrStartResponse>();
            if (result?.Content?.AudioPath is null)
            {
                StatusLabel.Text = "Không tìm thấy audio";
                return;
            }

            // Lưu sessionId để kết thúc sau
            _sessionId = result.Session?.Id ?? Guid.Empty;

            PoiNameLabel.Text = result.Content.PoiName;
            PoiCodeLabel.Text = result.Content.PoiCode;

            MediaPlayer.Source = MediaSource.FromUri(ResolveAudioUrl(result.Content.AudioPath));

            StatusLabel.Text = "Sẵn sàng phát";
            PlayPauseButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Lỗi kết nối";
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    private static string ResolveAudioUrl(string filePath)
    {
        if (filePath.StartsWith("http://") || filePath.StartsWith("https://"))
            return filePath;
        var baseUrl = AppConfig.ApiBaseUrl.Replace("/api/v1/", "").TrimEnd('/');
        return $"{baseUrl}/{filePath.TrimStart('/')}";
    }

    // Gọi API kết thúc session, tính tổng giây đã nghe
    private async Task EndSessionAsync()
    {
        if (_sessionId == Guid.Empty || _sessionEnded)
            return;

        _sessionEnded = true;

        // Cộng thêm thời gian đang phát dở (nếu có)
        if (_isPlaying && _playStartedAt != DateTime.MinValue)
            _totalPlayedSeconds += (int)(DateTime.UtcNow - _playStartedAt).TotalSeconds;

        try
        {
            await _httpClient.PostAsJsonAsync(
                $"sessions/{_sessionId}/end",
                new { DurationSeconds = _totalPlayedSeconds });
        }
        catch { /* silent — không block UI */ }

        _sessionId = Guid.Empty;
    }

    private void OnPlayPauseClicked(object sender, EventArgs e)
    {
        if (_isPlaying)
        {
            // Tạm dừng — cộng dồn thời gian
            if (_playStartedAt != DateTime.MinValue)
                _totalPlayedSeconds += (int)(DateTime.UtcNow - _playStartedAt).TotalSeconds;

            MediaPlayer.Pause();
            PlayPauseButton.Text = "▶ Phát";
            StatusLabel.Text = "Đã tạm dừng";
            PlayingIcon.Text = "🎵";
            _isPlaying = false;
        }
        else
        {
            _playStartedAt = DateTime.UtcNow;
            MediaPlayer.Play();
            PlayPauseButton.Text = "⏸ Tạm dừng";
            StatusLabel.Text = "Đang phát...";
            PlayingIcon.Text = "🔊";
            _isPlaying = true;
        }
    }

    private async void OnStopClicked(object sender, EventArgs e)
    {
        if (_isPlaying && _playStartedAt != DateTime.MinValue)
            _totalPlayedSeconds += (int)(DateTime.UtcNow - _playStartedAt).TotalSeconds;

        _isPlaying = false;
        MediaPlayer.Stop();
        PlayPauseButton.Text = "▶ Phát";
        StatusLabel.Text = "Đã dừng";
        PlayingIcon.Text = "🎵";

        await EndSessionAsync();
    }

    private async void OnMediaEnded(object? sender, EventArgs e)
    {
        if (_playStartedAt != DateTime.MinValue)
            _totalPlayedSeconds += (int)(DateTime.UtcNow - _playStartedAt).TotalSeconds;

        _isPlaying = false;
        PlayPauseButton.Text = "▶ Phát lại";
        StatusLabel.Text = "Đã phát xong ✅";
        PlayingIcon.Text = "✅";

        await EndSessionAsync();
    }

    // Kết thúc session khi người dùng back ra khỏi trang
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (_isPlaying && _playStartedAt != DateTime.MinValue)
            _totalPlayedSeconds += (int)(DateTime.UtcNow - _playStartedAt).TotalSeconds;
        _isPlaying = false;
        await EndSessionAsync();
    }

    private async void OnLanguageViClicked(object sender, EventArgs e)
    {
        _currentLanguage = "vi";
        BtnVi.BackgroundColor = Color.FromArgb("#EC4899");
        BtnVi.TextColor = Colors.White;
        BtnEn.BackgroundColor = Color.FromArgb("#FBCFE8");
        BtnEn.TextColor = Color.FromArgb("#9D174D");
        MediaPlayer.Stop();
        _isPlaying = false;
        _loadRequested = false; // reset để cho phép load lại
        await LoadAudioAsync();
    }

    private async void OnLanguageEnClicked(object sender, EventArgs e)
    {
        _currentLanguage = "en";
        BtnEn.BackgroundColor = Color.FromArgb("#EC4899");
        BtnEn.TextColor = Colors.White;
        BtnVi.BackgroundColor = Color.FromArgb("#FBCFE8");
        BtnVi.TextColor = Color.FromArgb("#9D174D");
        MediaPlayer.Stop();
        _isPlaying = false;
        _loadRequested = false; // reset để cho phép load lại
        await LoadAudioAsync();
    }
}

public class QrStartResponse
{
    public QrSessionInfo? Session { get; set; }
    public QrContentInfo? Content { get; set; }
}

public class QrSessionInfo
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PoiId { get; set; }
}

public class QrContentInfo
{
    public Guid PoiId { get; set; }
    public string PoiCode { get; set; } = string.Empty;
    public string PoiName { get; set; } = string.Empty;
    public string AudioPath { get; set; } = string.Empty;
    public bool IsTextToSpeech { get; set; }
}
