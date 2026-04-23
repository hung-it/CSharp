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
        _httpClient = AppConfig.CreateHttpClient();
    }

    private async Task LoadAudioAsync()
    {
        if (string.IsNullOrEmpty(_qrPayload) || string.IsNullOrEmpty(_userId))
        {
            UpdateStatus("Lỗi: Thiếu thông tin");
            return;
        }

        // Kết thúc session cũ nếu đang đổi ngôn ngữ
        try
        {
            await EndSessionAsync();
        }
        catch { /* ignore */ }

        UpdateStatus("Đang tải...");
        SetControlsEnabled(false);
        _sessionEnded = false;
        _totalPlayedSeconds = 0;

        try
        {
            if (!Guid.TryParse(_userId, out var userId))
            {
                UpdateStatus("Lỗi: userId không hợp lệ");
                await DisplayAlertAsync("Lỗi", "ID người dùng không hợp lệ", "OK");
                return;
            }

            var response = await _httpClient.PostAsJsonAsync("qr/start", new
            {
                UserId = userId,
                QrPayload = _qrPayload,
                LanguageCode = _currentLanguage
            });

            if (!response.IsSuccessStatusCode)
            {
                UpdateStatus("Lỗi tải audio");
                var errText = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"QR Start error: {errText}");
                await DisplayAlertAsync("Lỗi", "Không thể tải audio. Vui lòng thử lại.", "OK");
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<QrStartResponse>();
            
            if (result?.Content == null)
            {
                UpdateStatus("Không có audio");
                await DisplayAlertAsync("Thông báo", "Audio chưa được cung cấp cho điểm này.", "OK");
                return;
            }

            // Lưu sessionId để kết thúc sau
            _sessionId = result.Session?.Id ?? Guid.Empty;

            PoiNameLabel.Text = result.Content.PoiName ?? "Điểm tham quan";
            PoiCodeLabel.Text = result.Content.PoiCode ?? "";

            var audioPath = result.Content.AudioPath;
            if (string.IsNullOrEmpty(audioPath))
            {
                UpdateStatus("Chưa có audio");
                PlayingIcon.Text = "📝";
                await DisplayAlertAsync("Thông báo", "Audio chưa được cung cấp. Vui lòng liên hệ quản trị viên.", "OK");
                return;
            }

            // Resolve audio URL
            var audioUrl = ResolveAudioUrl(audioPath);
            System.Diagnostics.Debug.WriteLine($"Loading audio from: {audioUrl}");
            
            // Set media source with error handling
            try
            {
                MediaPlayer.Source = MediaSource.FromUri(audioUrl);
                UpdateStatus("Sẵn sàng phát");
                SetControlsEnabled(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MediaPlayer source error: {ex.Message}");
                UpdateStatus("Lỗi định dạng audio");
                await DisplayAlertAsync("Lỗi", $"Không thể tải file audio: {ex.Message}", "OK");
            }
        }
        catch (HttpRequestException ex)
        {
            UpdateStatus("Lỗi kết nối");
            PlayingIcon.Text = "❌";
            System.Diagnostics.Debug.WriteLine($"HTTP error: {ex.Message}");
            await DisplayAlertAsync("Lỗi kết nối", $"Không thể kết nối server: {ex.Message}", "OK");
        }
        catch (Exception ex)
        {
            UpdateStatus("Lỗi");
            PlayingIcon.Text = "❌";
            System.Diagnostics.Debug.WriteLine($"Audio load error: {ex.Message}");
            await DisplayAlertAsync("Lỗi", $"Đã xảy ra lỗi: {ex.Message}", "OK");
        }
    }

    private void UpdateStatus(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = message;
        });
    }

    private void SetControlsEnabled(bool enabled)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PlayPauseButton.IsEnabled = enabled;
        });
    }

    private static string ResolveAudioUrl(string filePath)
    {
        if (filePath.StartsWith("http://") || filePath.StartsWith("https://"))
            return filePath;
        var baseUrl = AppConfig.MediaBaseUrl;
        return $"{baseUrl}/{filePath.TrimStart('/')}";
    }

    private async Task EndSessionAsync()
    {
        if (_sessionId == Guid.Empty || _sessionEnded)
            return;

        _sessionEnded = true;

        // Cộng thêm thời gian đang phát dở (nếu có)
        if (_isPlaying && _playStartedAt != DateTime.MinValue)
            _totalPlayedSeconds += (int)(DateTime.UtcNow - _playStartedAt).TotalSeconds;

        // Record listening duration for visit tracking
        TrackingService.RecordListeningEnd(_totalPlayedSeconds);

        try
        {
            await _httpClient.PostAsJsonAsync(
                $"sessions/{_sessionId}/end",
                new { DurationSeconds = _totalPlayedSeconds });
        }
        catch { /* silent — không block UI */ }

        _sessionId = Guid.Empty;
    }

    private void OnPlayPauseClicked(object? sender, EventArgs e)
    {
        try
        {
            if (_isPlaying)
            {
                // Tạm dừng — cộng dồn thời gian
                if (_playStartedAt != DateTime.MinValue)
                    _totalPlayedSeconds += (int)(DateTime.UtcNow - _playStartedAt).TotalSeconds;

                MediaPlayer?.Pause();
                PlayPauseButton.Text = "▶ Phát";
                UpdateStatus("Đã tạm dừng");
                PlayingIcon.Text = "🎵";
                _isPlaying = false;
            }
            else
            {
                _playStartedAt = DateTime.UtcNow;
                MediaPlayer?.Play();
                PlayPauseButton.Text = "⏸ Tạm dừng";
                UpdateStatus("Đang phát...");
                PlayingIcon.Text = "🔊";
                _isPlaying = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Play/Pause error: {ex.Message}");
            UpdateStatus("Lỗi phát audio");
        }
    }

    private async void OnStopClicked(object? sender, EventArgs e)
    {
        try
        {
            if (_isPlaying && _playStartedAt != DateTime.MinValue)
                _totalPlayedSeconds += (int)(DateTime.UtcNow - _playStartedAt).TotalSeconds;

            _isPlaying = false;
            MediaPlayer?.Stop();
            PlayPauseButton.Text = "▶ Phát";
            UpdateStatus("Đã dừng");
            PlayingIcon.Text = "🎵";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Stop error: {ex.Message}");
        }

        await EndSessionAsync();
    }

    private async void OnMediaEnded(object? sender, EventArgs e)
    {
        try
        {
            if (_playStartedAt != DateTime.MinValue)
                _totalPlayedSeconds += (int)(DateTime.UtcNow - _playStartedAt).TotalSeconds;

            _isPlaying = false;
            PlayPauseButton.Text = "▶ Phát lại";
            UpdateStatus("Đã phát xong ✅");
            PlayingIcon.Text = "✅";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Media ended error: {ex.Message}");
        }

        await EndSessionAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        try
        {
            if (_isPlaying && _playStartedAt != DateTime.MinValue)
                _totalPlayedSeconds += (int)(DateTime.UtcNow - _playStartedAt).TotalSeconds;
            _isPlaying = false;
            MediaPlayer?.Stop();
        }
        catch { /* ignore */ }
        await EndSessionAsync();
    }

    private async void OnLanguageViClicked(object? sender, EventArgs e)
    {
        _currentLanguage = "vi";
        BtnVi.BackgroundColor = Color.FromArgb("#EC4899");
        BtnVi.TextColor = Colors.White;
        BtnEn.BackgroundColor = Color.FromArgb("#FBCFE8");
        BtnEn.TextColor = Color.FromArgb("#9D174D");
        try { MediaPlayer?.Stop(); } catch { }
        _isPlaying = false;
        _loadRequested = false; // reset để cho phép load lại
        await LoadAudioAsync();
    }

    private async void OnLanguageEnClicked(object? sender, EventArgs e)
    {
        _currentLanguage = "en";
        BtnEn.BackgroundColor = Color.FromArgb("#EC4899");
        BtnEn.TextColor = Colors.White;
        BtnVi.BackgroundColor = Color.FromArgb("#FBCFE8");
        BtnVi.TextColor = Color.FromArgb("#9D174D");
        try { MediaPlayer?.Stop(); } catch { }
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
