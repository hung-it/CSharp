using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

public partial class RegistrationPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public RegistrationPage()
    {
        InitializeComponent();
        _httpClient = AppConfig.CreateHttpClient();
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        var username = UsernameEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;
        var confirm = ConfirmPasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(username))
        {
            ShowStatus("Vui lòng nhập tên đăng nhập.", isError: true);
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowStatus("Vui lòng nhập mật khẩu.", isError: true);
            return;
        }

        if (password != confirm)
        {
            ShowStatus("Mật khẩu xác nhận không khớp.", isError: true);
            return;
        }

        if (username.Length < 3)
        {
            ShowStatus("Tên đăng nhập phải có ít nhất 3 ký tự.", isError: true);
            return;
        }

        if (password.Length < 4)
        {
            ShowStatus("Mật khẩu phải có ít nhất 4 ký tự.", isError: true);
            return;
        }

        await PerformRegisterAsync(username, password);
    }

    private async void OnBackToLoginTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//login");
    }

    private async Task PerformRegisterAsync(string username, string password)
    {
        try
        {
            SetLoading(true);
            HideStatus();

            var response = await _httpClient.PostAsJsonAsync("users/register", new
            {
                username,
                password,
                preferredLanguage = AppConfig.DefaultPreferredLanguage
            });

            var result = await response.Content.ReadFromJsonAsync<RegisterResult>();

            if (result?.Success == true)
            {
                ShowStatus("Đăng ký thành công! Đang chuyển đến trang đăng nhập...", isError: false);
                await Task.Delay(1500);
                await Shell.Current.GoToAsync("//login");
            }
            else
            {
                ShowStatus(result?.Message ?? "Đăng ký thất bại. Vui lòng thử lại.", isError: true);
            }
        }
        catch (HttpRequestException)
        {
            ShowStatus("Không thể kết nối server. Vui lòng kiểm tra kết nối mạng.", isError: true);
        }
        catch (Exception ex)
        {
            ShowStatus($"Lỗi: {ex.Message}", isError: true);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusLabel.Text = (isError ? " " : " ") + message;
        StatusLabel.TextColor = isError ? Color.FromArgb("#EF4444") : Color.FromArgb("#10B981");
        StatusLabel.IsVisible = true;
    }

    private void HideStatus()
    {
        StatusLabel.IsVisible = false;
    }

    private void SetLoading(bool isLoading)
    {
        LoadingIndicator.IsRunning = isLoading;
        LoadingIndicator.IsVisible = isLoading;
        RegisterBtn.IsEnabled = !isLoading;
        UsernameEntry.IsEnabled = !isLoading;
        PasswordEntry.IsEnabled = !isLoading;
        ConfirmPasswordEntry.IsEnabled = !isLoading;
    }
}

public class RegisterResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string? Plan { get; set; }
}
