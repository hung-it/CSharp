using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;
using System.Net.Http.Json;
using System.Timers;

namespace VinhKhanhAudioGuide.App;

[QueryProperty(nameof(QrPayload), "qr")]
[QueryProperty(nameof(UserId), "userId")]
public partial class AudioPlayerPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _audioPlayer;
    private System.Timers.Timer? _progressTimer;

    private string _currentLanguage = "vi";
    private string _qrPayload = string.Empty;
    private string _userId = string.Empty;
    private string _currentAudioUrl = string.Empty;
    private string _currentAudioFilePath = string.Empty;
    private Guid _sessionId = Guid.Empty;
    private int _totalPlayedSeconds = 0;
    private bool _sessionEnded = false;
    private bool _loadRequested = false;
    private bool _isPlaying = false;
    private CancellationTokenSource? _downloadCts;

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
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try { await LoadAudioAsync(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryLoadAudio error: {ex.Message}");
                UpdateStatus("❌ Lỗi tải audio");
            }
        });
    }

    public AudioPlayerPage(IAudioManager audioManager)
    {
        InitializeComponent();
        _httpClient = AppConfig.CreateHttpClient();
        _audioManager = audioManager;

        _progressTimer = new System.Timers.Timer(500);
        _progressTimer.Elapsed += OnProgressTimerElapsed;
    }

    private void OnProgressTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_audioPlayer != null && _audioPlayer.Duration > 0)
            {
                double progress = _audioPlayer.CurrentPosition / _audioPlayer.Duration * 100;
                ProgressSlider.Value = progress;

                var current = TimeSpan.FromSeconds(_audioPlayer.CurrentPosition);
                var total = TimeSpan.FromSeconds(_audioPlayer.Duration);
                DurationLabel.Text = $"{current:mm\\:ss} / {total:mm\\:ss}";
            }
        });
    }

    private async Task<string?> DownloadAudioToLocalAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var cacheDir = Path.Combine(FileSystem.CacheDirectory, "audio_cache");
            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);

            var fileName = Path.GetFileName(new Uri(url).LocalPath);
            var filePath = Path.Combine(cacheDir, fileName);

            if (File.Exists(filePath))
            {
                var info = new FileInfo(filePath);
                if (info.Length > 1000)
                {
                    System.Diagnostics.Debug.WriteLine($"Using cached audio: {filePath}");
                    return filePath;
                }
            }

            UpdateStatus("⏳ Đang tải audio...");

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(filePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if (canReportProgress && totalBytes > 0)
                {
                    var percent = (int)((totalRead * 100) / totalBytes);
                    UpdateStatus($"⏳ Đang tải... {percent}%");
                }
            }

            System.Diagnostics.Debug.WriteLine($"Downloaded audio to: {filePath} ({totalRead} bytes)");
            return filePath;
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("Audio download cancelled");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DownloadAudioToLocal error: {ex.Message}");
            return null;
        }
    }

    private async Task LoadAudioAsync()
    {
        if (string.IsNullOrEmpty(_qrPayload) || string.IsNullOrEmpty(_userId))
        {
            UpdateStatus("❌ Lỗi: Thiếu thông tin");
            return;
        }

        _downloadCts?.Cancel();
        _downloadCts = new CancellationTokenSource();

        try { await EndSessionAsync(); }
        catch { }

        StopPlayback();
        UpdateStatus("⏳ Đang tải...");
        SetControlsEnabled(false);
        _sessionEnded = false;
        _totalPlayedSeconds = 0;

        try
        {
            if (!Guid.TryParse(_userId, out var userId))
            {
                UpdateStatus("❌ Lỗi: userId không hợp lệ");
                await DisplayAlertAsync("❌ Lỗi", "ID người dùng không hợp lệ", "OK");
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
                UpdateStatus("❌ Lỗi tải audio");
                var errText = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"QR Start error: {errText}");
                await DisplayAlertAsync("❌ Lỗi", $"Không thể tải audio. Code: {(int)response.StatusCode}", "OK");
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<QrStartResponse>();

            if (result?.Content == null)
            {
                UpdateStatus("📭 Không có audio");
                await DisplayAlertAsync("📢 Thông báo", "Audio chưa được cung cấp cho điểm này.", "OK");
                return;
            }

            _sessionId = result.Session?.Id ?? Guid.Empty;
            PoiNameLabel.Text = result.Content.PoiName ?? "Điểm tham quan";
            PoiCodeLabel.Text = result.Content.PoiCode ?? "";

            var audioPath = result.Content.AudioPath;
            if (string.IsNullOrEmpty(audioPath))
            {
                UpdateStatus("📭 Chưa có audio");
                PlayingIcon.Text = "❌";
                await DisplayAlertAsync("📢 Thông báo", "Audio chưa được cung cấp. Vui lòng liên hệ quản trị viên.", "OK");
                return;
            }

            _currentAudioUrl = ResolveAudioUrl(audioPath);
            System.Diagnostics.Debug.WriteLine($"Loading audio from: {_currentAudioUrl}");

            UpdateStatus("⏳ Đang tải audio...");

            var localFilePath = await DownloadAudioToLocalAsync(_currentAudioUrl, _downloadCts.Token);

            if (string.IsNullOrEmpty(localFilePath))
            {
                UpdateStatus("❌ Lỗi tải audio");
                await DisplayAlertAsync("❌ Lỗi", "Không thể tải file audio. Kiểm tra kết nối mạng.", "OK");
                return;
            }

            _currentAudioFilePath = localFilePath;

            StopPlayback();

            try
            {
                _audioPlayer = _audioManager.CreatePlayer(_currentAudioFilePath);
                _audioPlayer.PlaybackEnded += OnPlaybackEnded;

                var total = TimeSpan.FromSeconds(_audioPlayer.Duration);
                DurationLabel.Text = $"0:00 / {total:mm\\:ss}";
                ProgressSlider.Maximum = 100;
                ProgressSlider.Value = 0;
                ProgressSlider.IsEnabled = true;

                UpdateStatus("✅ Sẵn sàng - nhấn Phát để nghe");
                SetControlsEnabled(true);
                PlayingIcon.Text = "🎧";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audio player error: {ex.Message}");
                UpdateStatus("❌ Lỗi tải audio");
                await DisplayAlertAsync("❌ Lỗi", $"Không thể phát audio: {ex.Message}", "OK");
            }
        }
        catch (OperationCanceledException)
        {
            UpdateStatus("⏹️ Đã hủy");
        }
        catch (HttpRequestException ex)
        {
            UpdateStatus("❌ Lỗi kết nối");
            PlayingIcon.Text = "❌";
            System.Diagnostics.Debug.WriteLine($"HTTP error: {ex.Message}");
            await DisplayAlertAsync("❌ Lỗi kết nối", $"Không thể kết nối server: {ex.Message}", "OK");
        }
        catch (Exception ex)
        {
            UpdateStatus("❌ Lỗi");
            PlayingIcon.Text = "❌";
            System.Diagnostics.Debug.WriteLine($"Audio load error: {ex.Message}");
            await DisplayAlertAsync("❌ Lỗi", $"Đã xảy ra lỗi: {ex.Message}", "OK");
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
        TrackingService.RecordListeningEnd(_totalPlayedSeconds);

        try
        {
            await _httpClient.PostAsJsonAsync(
                $"sessions/{_sessionId}/end",
                new { DurationSeconds = _totalPlayedSeconds });
        }
        catch { }

        _sessionId = Guid.Empty;
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        _isPlaying = false;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PlayPauseButton.Text = "▶️ Phát";
            UpdateStatus("🎉 Đã phát xong!");
            PlayingIcon.Text = "✅";
            _progressTimer?.Stop();
            ProgressSlider.Value = 0;
        });
    }

    private void StopPlayback()
    {
        _progressTimer?.Stop();
        if (_audioPlayer != null)
        {
            try
            {
                _audioPlayer.PlaybackEnded -= OnPlaybackEnded;
                _audioPlayer.Stop();
                _audioPlayer.Dispose();
            }
            catch { }
            _audioPlayer = null;
        }
    }

    private void OnPlayPauseClicked(object? sender, EventArgs e)
    {
        try
        {
            if (_audioPlayer == null)
            {
                UpdateStatus("❌ Chưa có audio");
                return;
            }

            if (_isPlaying)
            {
                _audioPlayer.Pause();
                _progressTimer?.Stop();
                _isPlaying = false;
                PlayPauseButton.Text = "▶️ Phát";
                UpdateStatus("⏸️ Đã tạm dừng");
                PlayingIcon.Text = "⏸️";
            }
            else
            {
                _audioPlayer.Play();
                _progressTimer?.Start();
                _isPlaying = true;
                PlayPauseButton.Text = "⏸️ Tạm dừng";
                UpdateStatus("🎵 Đang phát...");
                PlayingIcon.Text = "🎵";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Play/Pause error: {ex.Message}");
            UpdateStatus("❌ Lỗi phát audio");
        }
    }

    private async void OnStopClicked(object? sender, EventArgs e)
    {
        try
        {
            StopPlayback();
            _isPlaying = false;
            PlayPauseButton.Text = "▶️ Phát";
            UpdateStatus("⏹️ Đã dừng");
            PlayingIcon.Text = "🎧";
            ProgressSlider.Value = 0;
            DurationLabel.Text = "0:00 / 0:00";
            _totalPlayedSeconds = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Stop error: {ex.Message}");
        }

        await EndSessionAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        _downloadCts?.Cancel();

        if (_isPlaying && _audioPlayer != null)
        {
            _totalPlayedSeconds += (int)_audioPlayer.CurrentPosition;
        }

        StopPlayback();
        await EndSessionAsync();
    }

    private async void OnLanguageViClicked(object? sender, EventArgs e)
    {
        _currentLanguage = "vi";
        BtnVi.BackgroundColor = Color.FromArgb("#EC4899");
        BtnVi.TextColor = Colors.White;
        BtnEn.BackgroundColor = Color.FromArgb("#FBCFE8");
        BtnEn.TextColor = Color.FromArgb("#9D174D");
        _loadRequested = false;
        _currentAudioUrl = string.Empty;
        _currentAudioFilePath = string.Empty;
        await LoadAudioAsync();
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
        BtnEn.BackgroundColor = Color.FromArgb("#EC4899");
        BtnEn.TextColor = Colors.White;
        BtnVi.BackgroundColor = Color.FromArgb("#FBCFE8");
        BtnVi.TextColor = Color.FromArgb("#9D174D");
        _loadRequested = false;
        _currentAudioUrl = string.Empty;
        _currentAudioFilePath = string.Empty;
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
