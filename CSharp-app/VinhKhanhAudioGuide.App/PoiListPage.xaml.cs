using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace VinhKhanhAudioGuide.App
{
    public partial class PoiListPage : ContentPage
    {
        private const string SEARCH_PLACEHOLDER = "🔍 Tìm kiếm POI...";
        private const string NO_RESULTS = "😢 Không tìm thấy POI";
        private const string TRY_DIFFERENT_FILTERS = "Thử thay đổi bộ lọc hoặc từ khóa tìm kiếm";
        private const string LOADING = "⏳ Đang tải...";
        private const string LOAD_COMPLETE = "✅ Đã tải xong";
        private const string LOAD_ERROR = "❌ Lỗi tải dữ liệu";
        private const string FILTER_ALL = "Tất cả";
        private const string FILTER_XOM_CHIEU = "Xóm Chiếu";
        private const string FILTER_VINH_HOI = "Vĩnh Hội";
        private const string FILTER_KHANH_HOI = "Khánh Hội";
        private const string SORT_DEFAULT = "Mặc định";
        private const string SORT_NAME = "Theo tên";
        private const string SORT_NEAREST = "Gần nhất";
        private const string SORT_POPULAR = "Phổ biến";

        private string currentFilter = "all";
        private string currentSort = SORT_DEFAULT;
        private List<PoiItem> allPois = new List<PoiItem>();

        public PoiListPage()
        {
            InitializeComponent();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            EmptyStateFrame.IsVisible = false;

            try
            {
                await LoadPoisAsync();
            }
            catch (Exception)
            {
                await DisplayAlert("❌", LOAD_ERROR, "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private async Task LoadPoisAsync()
        {
            // Sample POI data
            allPois = new List<PoiItem>
            {
                new PoiItem { Id = "1", Name = "Quán ăn Vĩnh Hạnh", Category = "🏠 Nhà hàng", District = "vinhhoi", Rating = 4.8, Popularity = 95 },
                new PoiItem { Id = "2", Name = "Bánh xèo Tư Hòa", Category = "🍜 Ẩm thực", District = "xomchieu", Rating = 4.6, Popularity = 88 },
                new PoiItem { Id = "3", Name = "Chùa Vĩnh Nghiêm", Category = "🏛️ Di tích", District = "vinhhoi", Rating = 4.9, Popularity = 92 },
                new PoiItem { Id = "4", Name = "Cà phê Sữa Đá", Category = "☕ Quán cà phê", District = "khanhhoi", Rating = 4.5, Popularity = 85 },
                new PoiItem { Id = "5", Name = "Bún bò Huế", Category = "🍜 Ẩm thực", District = "khanhhoi", Rating = 4.7, Popularity = 90 },
                new PoiItem { Id = "6", Name = "Miến lươn Vạn Long", Category = "🍜 Ẩm thực", District = "vinhhoi", Rating = 4.4, Popularity = 78 },
                new PoiItem { Id = "7", Name = "Chợ Xóm Chiếu", Category = "🏪 Chợ", District = "xomchieu", Rating = 4.2, Popularity = 75 },
                new PoiItem { Id = "8", Name = "Nhà thờ Vĩnh Hội", Category = "🏛️ Di tích", District = "vinhhoi", Rating = 4.6, Popularity = 82 },
                new PoiItem { Id = "9", Name = "Bánh tráng nướng", Category = "🍜 Ẩm thực", District = "khanhhoi", Rating = 4.3, Popularity = 80 },
                new PoiItem { Id = "10", Name = "Trà sữa Vĩnh Khách", Category = "🧋 Đồ uống", District = "xomchieu", Rating = 4.5, Popularity = 88 },
            };

            await Task.Delay(500); // Simulate loading
            ApplyFiltersAndSort();
        }

        private void ApplyFiltersAndSort()
        {
            var filteredPois = allPois.AsEnumerable();

            // Apply filter
            switch (currentFilter.ToLower())
            {
                case "xomchieu":
                    filteredPois = filteredPois.Where(p => p.District == "xomchieu");
                    break;
                case "vinhhoi":
                    filteredPois = filteredPois.Where(p => p.District == "vinhhoi");
                    break;
                case "khanhhoi":
                    filteredPois = filteredPois.Where(p => p.District == "khanhhoi");
                    break;
            }

            // Apply search
            var searchText = SearchEntry.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredPois = filteredPois.Where(p => 
                    p.Name.ToLower().Contains(searchText) || 
                    p.Category.ToLower().Contains(searchText));
            }

            // Apply sort
            switch (currentSort)
            {
                case SORT_NAME:
                    filteredPois = filteredPois.OrderBy(p => p.Name);
                    break;
                case SORT_NEAREST:
                    filteredPois = filteredPois.OrderBy(p => p.Distance);
                    break;
                case SORT_POPULAR:
                    filteredPois = filteredPois.OrderByDescending(p => p.Popularity);
                    break;
            }

            var pois = filteredPois.ToList();
            DisplayPois(pois);
        }

        private void DisplayPois(List<PoiItem> pois)
        {
            PoiListContainer.Children.Clear();

            if (pois.Count == 0)
            {
                EmptyStateFrame.IsVisible = true;
                return;
            }

            EmptyStateFrame.IsVisible = false;

            foreach (var poi in pois)
            {
                var card = CreatePoiCard(poi);
                PoiListContainer.Children.Add(card);
            }
        }

        private Frame CreatePoiCard(PoiItem poi)
        {
            var card = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 16,
                Padding = 16,
                HasShadow = true
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var leftStack = new StackLayout();
            
            var categoryLabel = new Label
            {
                Text = poi.Category,
                FontSize = 12,
                TextColor = Color.FromArgb("#FF69B4")
            };

            var nameLabel = new Label
            {
                Text = poi.Name,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#333333")
            };

            var ratingStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var starLabel = new Label
            {
                Text = "⭐",
                FontSize = 14
            };

            var ratingLabel = new Label
            {
                Text = poi.Rating.ToString("F1"),
                FontSize = 14,
                TextColor = Color.FromArgb("#666666"),
                Margin = new Thickness(4, 0, 0, 0)
            };

            var distanceLabel = new Label
            {
                Text = $"📍 {poi.Distance:F1} km",
                FontSize = 12,
                TextColor = Color.FromArgb("#999999"),
                Margin = new Thickness(8, 0, 0, 0)
            };

            ratingStack.Children.Add(starLabel);
            ratingStack.Children.Add(ratingLabel);
            ratingStack.Children.Add(distanceLabel);

            leftStack.Children.Add(categoryLabel);
            leftStack.Children.Add(nameLabel);
            leftStack.Children.Add(ratingStack);

            var rightStack = new StackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };

            var playButton = new Button
            {
                Text = "🎧",
                FontSize = 24,
                BackgroundColor = Color.FromArgb("#FF69B4"),
                TextColor = Colors.White,
                CornerRadius = 25,
                WidthRequest = 50,
                HeightRequest = 50
            };
            playButton.Clicked += async (s, e) =>
            {
                await Navigation.PushAsync(new AudioPlayerPage(poi.Id, poi.Name));
            };

            rightStack.Children.Add(playButton);

            grid.Children.Add(leftStack, 0, 0);
            grid.Children.Add(rightStack, 1, 0);

            card.Content = grid;

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) =>
            {
                await Navigation.PushAsync(new AudioPlayerPage(poi.Id, poi.Name));
            };
            card.GestureRecognizers.Add(tapGesture);

            return card;
        }

        private void SearchEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndSort();
        }

        private void AllFilterButton_Clicked(object sender, EventArgs e)
        {
            currentFilter = "all";
            UpdateFilterButtons();
            ApplyFiltersAndSort();
        }

        private void XomChieuFilterButton_Clicked(object sender, EventArgs e)
        {
            currentFilter = "xomchieu";
            UpdateFilterButtons();
            ApplyFiltersAndSort();
        }

        private void VinhHoiFilterButton_Clicked(object sender, EventArgs e)
        {
            currentFilter = "vinhhoi";
            UpdateFilterButtons();
            ApplyFiltersAndSort();
        }

        private void KhanhHoiFilterButton_Clicked(object sender, EventArgs e)
        {
            currentFilter = "khanhhoi";
            UpdateFilterButtons();
            ApplyFiltersAndSort();
        }

        private void UpdateFilterButtons()
        {
            var pinkColor = Color.FromArgb("#FF69B4");
            var lightPinkColor = Color.FromArgb("#FFB6C1");
            var darkText = Color.FromArgb("#333333");

            AllFilterButton.BackgroundColor = currentFilter == "all" ? pinkColor : lightPinkColor;
            AllFilterButton.TextColor = currentFilter == "all" ? Colors.White : darkText;

            XomChieuFilterButton.BackgroundColor = currentFilter == "xomchieu" ? pinkColor : lightPinkColor;
            XomChieuFilterButton.TextColor = currentFilter == "xomchieu" ? Colors.White : darkText;

            VinhHoiFilterButton.BackgroundColor = currentFilter == "vinhhoi" ? pinkColor : lightPinkColor;
            VinhHoiFilterButton.TextColor = currentFilter == "vinhhoi" ? Colors.White : darkText;

            KhanhHoiFilterButton.BackgroundColor = currentFilter == "khanhhoi" ? pinkColor : lightPinkColor;
            KhanhHoiFilterButton.TextColor = currentFilter == "khanhhoi" ? Colors.White : darkText;
        }

        private void SortPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SortPicker.SelectedIndex >= 0)
            {
                currentSort = SortPicker.Items[SortPicker.SelectedIndex];
                ApplyFiltersAndSort();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ApplyFiltersAndSort();
        }
    }
}
