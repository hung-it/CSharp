using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace VinhKhanhAudioGuide.App
{
    public partial class LoginPage : ContentPage
    {
        private const string USERNAME_REQUIRED = "⚠️ Vui lòng nhập tên đăng nhập";
        private const string PASSWORD_REQUIRED = "⚠️ Vui lòng nhập mật khẩu";
        private const string LOGIN_FAILED = "❌ Đăng nhập thất bại";
        private const string LOGIN_SUCCESS = "✅ Đăng nhập thành công!";
        private const string CONNECTION_ERROR = "❌ Không thể kết nối server";
        private const string DEMO_LOGIN = "🔑 Đang đăng nhập demo...";
        private const string WELCOME = "🎉 Chào mừng";
        private const string PREMIUM_DIALOG = "💎 Premium";
        private const string PREMIUM_MESSAGE = "✨ Bạn có thể nâng cấp lên Premium để sử dụng:\n\n💎 Audio tiếng Anh\n🗄️ Chế độ Offline\n📊 Thống kê nâng cao\n⚡ Ưu tiên hỗ trợ";
        private const string UPGRADE = "Nâng cấp ngay";
        private const string OK = "OK";
        private const string CONTINUE = "Tiếp tục";

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void LoginButton_Clicked(object sender, EventArgs e)
        {
            var username = UsernameEntry.Text?.Trim();
            var password = PasswordEntry.Text?.Trim();

            if (string.IsNullOrEmpty(username))
            {
                await DisplayAlert("⚠️", USERNAME_REQUIRED, OK);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                await DisplayAlert("⚠️", PASSWORD_REQUIRED, OK);
                return;
            }

            await PerformLoginAsync(username, password);
        }

        private async Task PerformLoginAsync(string username, string password)
        {
            LoginButton.IsEnabled = false;
            LoginActivityIndicator.IsVisible = true;
            LoginActivityIndicator.IsRunning = true;
            LoginButton.Text = "⏳ Đang đăng nhập...";

            try
            {
                // Simulate API call
                await Task.Delay(1500);

                // For demo, accept any credentials
                if (!string.IsNullOrEmpty(username))
                {
                    // Login successful
                    AppConfig.IsLoggedIn = true;
                    AppConfig.UserName = username;
                    AppConfig.UserId = Guid.NewGuid().ToString().Substring(0, 8);
                    AppConfig.AuthToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

                    await DisplayAlert("✅", $"{LOGIN_SUCCESS}\n{WELCOME}, {username}!", OK);
                    
                    // Go back to previous page or main page
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("❌", LOGIN_FAILED, OK);
                }
            }
            catch (Exception)
            {
                await DisplayAlert("❌", CONNECTION_ERROR, OK);
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginActivityIndicator.IsVisible = false;
                LoginActivityIndicator.IsRunning = false;
                LoginButton.Text = "🔑 Đăng nhập";
            }
        }

        private async void DemoButton_Clicked(object sender, EventArgs e)
        {
            DemoButton.IsEnabled = false;
            DemoButton.Text = DEMO_LOGIN;

            try
            {
                // Simulate demo login
                await Task.Delay(1000);

                // Set demo user
                AppConfig.IsLoggedIn = true;
                AppConfig.UserName = "Khách Demo";
                AppConfig.UserId = "DEMO-" + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
                AppConfig.AuthToken = "demo_token_" + Guid.NewGuid().ToString().Substring(0, 8);
                AppConfig.PlanType = "free";

                await DisplayAlert("✅", $"{LOGIN_SUCCESS}\n{WELCOME}, Khách Demo!", OK);
                
                await Navigation.PopAsync();
            }
            catch (Exception)
            {
                await DisplayAlert("❌", CONNECTION_ERROR, OK);
            }
            finally
            {
                DemoButton.IsEnabled = true;
                DemoButton.Text = "🎮 Dùng thử";
            }
        }

        private async void UpgradeButton_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert(PREMIUM_DIALOG, PREMIUM_MESSAGE, UPGRADE);
        }

        private async void SkipButton_Clicked(object sender, EventArgs e)
        {
            var result = await DisplayAlert("🚶", "Bạn có muốn tiếp tục mà không đăng nhập không?\n\nMột số tính năng sẽ bị giới hạn.", "Tiếp tục", "Hủy");
            
            if (result)
            {
                await Navigation.PopAsync();
            }
        }
    }
}
