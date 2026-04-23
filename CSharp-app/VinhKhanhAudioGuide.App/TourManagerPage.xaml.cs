using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace VinhKhanhAudioGuide.App
{
    public partial class TourManagerPage : ContentPage
    {
        private const string START_TOUR = "🚀 Bắt đầu tour";
        private const string TOUR_STARTED = "🎉 Đã bắt đầu tour!";
        private const string TOUR_MESSAGE = "Hành trình khám phá tuyến {0} đã bắt đầu.\n\n🎧 Nhớ bật audio guide để nghe thuyết minh nhé!";
        private const string XOM_CHIEU_TOUR = "Xóm Chiếu";
        private const string VINH_HOI_TOUR = "Vĩnh Hội";
        private const string KHANH_HOI_TOUR = "Khánh Hội";
        private const string OK = "OK";

        public TourManagerPage()
        {
            InitializeComponent();
            LoadTourStats();
        }

        private void LoadTourStats()
        {
            // Load user tour stats
            ToursCompletedLabel.Text = AppConfig.VisitedCount.ToString();
            PoisVisitedLabel.Text = AppConfig.VisitedCount.ToString();
            AudioListenedLabel.Text = AppConfig.ListenCount.ToString();

            // Update tour stops based on stats
            UpdateTourStats();
        }

        private void UpdateTourStats()
        {
            // Update stops based on visited count
            var visitedPois = AppConfig.VisitedCount;
            
            // Calculate remaining stops
            XomChieuStopsLabel.Text = Math.Max(0, 5 - visitedPois).ToString();
            VinhHoiStopsLabel.Text = Math.Max(0, 7 - visitedPois).ToString();
            KhanhHoiStopsLabel.Text = Math.Max(0, 6 - visitedPois).ToString();
        }

        private async void StartXomChieuTour_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var originalText = button.Text;
            
            button.Text = "⏳ Đang khởi động...";
            button.IsEnabled = false;

            try
            {
                await Task.Delay(500);
                await DisplayAlert("🎉", string.Format(TOUR_MESSAGE, XOM_CHIEU_TOUR), OK);
                button.Text = "✅ Đã bắt đầu";
                
                // Update stats
                AppConfig.VisitedCount++;
                ToursCompletedLabel.Text = AppConfig.VisitedCount.ToString();
            }
            catch (Exception)
            {
                button.Text = originalText;
            }
            finally
            {
                await Task.Delay(2000);
                button.Text = START_TOUR;
                button.IsEnabled = true;
            }
        }

        private async void StartVinhHoiTour_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var originalText = button.Text;
            
            button.Text = "⏳ Đang khởi động...";
            button.IsEnabled = false;

            try
            {
                await Task.Delay(500);
                await DisplayAlert("🎉", string.Format(TOUR_MESSAGE, VINH_HOI_TOUR), OK);
                button.Text = "✅ Đã bắt đầu";
                
                // Update stats
                AppConfig.VisitedCount++;
                ToursCompletedLabel.Text = AppConfig.VisitedCount.ToString();
            }
            catch (Exception)
            {
                button.Text = originalText;
            }
            finally
            {
                await Task.Delay(2000);
                button.Text = START_TOUR;
                button.IsEnabled = true;
            }
        }

        private async void StartKhanhHoiTour_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var originalText = button.Text;
            
            button.Text = "⏳ Đang khởi động...";
            button.IsEnabled = false;

            try
            {
                await Task.Delay(500);
                await DisplayAlert("🎉", string.Format(TOUR_MESSAGE, KHANH_HOI_TOUR), OK);
                button.Text = "✅ Đã bắt đầu";
                
                // Update stats
                AppConfig.VisitedCount++;
                ToursCompletedLabel.Text = AppConfig.VisitedCount.ToString();
            }
            catch (Exception)
            {
                button.Text = originalText;
            }
            finally
            {
                await Task.Delay(2000);
                button.Text = START_TOUR;
                button.IsEnabled = true;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadTourStats();
        }
    }
}
