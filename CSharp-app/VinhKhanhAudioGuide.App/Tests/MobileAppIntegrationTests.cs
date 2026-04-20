using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Testing;
using Xunit;

namespace VinhKhanhAudioGuide.App.Tests
{
    public class MobileAppIntegrationTests : IClassFixture<MauiAppFixture>
    {
        private readonly MauiAppFixture _fixture;

        public MobileAppIntegrationTests(MauiAppFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task MainPage_Should_Load_Successfully()
        {
            // Arrange
            var app = _fixture.Services.GetRequiredService<App>();
            
            // Act
            var mainPage = new MainPage();
            
            // Assert
            Assert.NotNull(mainPage);
            Assert.NotNull(mainPage.Content);
        }

        [Fact]
        public async Task QrScanPage_Should_Process_Valid_QR_Code()
        {
            // Arrange
            var qrScanPage = new QrScanPage();
            var testQrData = "https://api.example.com/poi/123";
            
            // Act
            await qrScanPage.ProcessQrCodeAsync(testQrData);
            
            // Assert
            // Verify navigation to POI detail page
            Assert.True(qrScanPage.IsNavigationCompleted);
        }

        [Fact]
        public async Task AudioPlayerPage_Should_Play_Audio()
        {
            // Arrange
            var audioPlayerPage = new AudioPlayerPage();
            var testAudioUrl = "https://api.example.com/audio/test.mp3";
            
            // Act
            await audioPlayerPage.LoadAudioAsync(testAudioUrl);
            await audioPlayerPage.PlayAsync();
            
            // Assert
            Assert.True(audioPlayerPage.IsPlaying);
            Assert.True(audioPlayerPage.Duration > TimeSpan.Zero);
        }

        [Fact]
        public async Task MapPage_Should_Display_POIs()
        {
            // Arrange
            var mapPage = new MapPage();
            var testPois = new List<PoiModel>
            {
                new PoiModel { Id = 1, Name = "Test POI 1", Latitude = 10.123, Longitude = 106.456 },
                new PoiModel { Id = 2, Name = "Test POI 2", Latitude = 10.124, Longitude = 106.457 }
            };
            
            // Act
            await mapPage.LoadPoisAsync(testPois);
            
            // Assert
            Assert.Equal(2, mapPage.DisplayedPoisCount);
            Assert.True(mapPage.IsMapInitialized);
        }

        [Fact]
        public async Task TourDetailPage_Should_Load_Tour_Data()
        {
            // Arrange
            var tourDetailPage = new TourDetailPage();
            var testTourId = 123;
            
            // Act
            await tourDetailPage.LoadTourAsync(testTourId);
            
            // Assert
            Assert.NotNull(tourDetailPage.CurrentTour);
            Assert.Equal(testTourId, tourDetailPage.CurrentTour.Id);
            Assert.True(tourDetailPage.CurrentTour.Pois.Count > 0);
        }

        [Fact]
        public async Task PoiListPage_Should_Filter_POIs()
        {
            // Arrange
            var poiListPage = new PoiListPage();
            var testPois = new List<PoiModel>
            {
                new PoiModel { Id = 1, Name = "Temple A", Category = "Religious" },
                new PoiModel { Id = 2, Name = "Restaurant B", Category = "Food" },
                new PoiModel { Id = 3, Name = "Temple C", Category = "Religious" }
            };
            
            // Act
            await poiListPage.LoadPoisAsync(testPois);
            poiListPage.ApplyFilter("Religious");
            
            // Assert
            Assert.Equal(2, poiListPage.FilteredPois.Count);
            Assert.All(poiListPage.FilteredPois, poi => Assert.Equal("Religious", poi.Category));
        }

        [Fact]
        public async Task TourManagerPage_Should_Download_Tour()
        {
            // Arrange
            var tourManagerPage = new TourManagerPage();
            var testTourId = 456;
            
            // Act
            var downloadResult = await tourManagerPage.DownloadTourAsync(testTourId);
            
            // Assert
            Assert.True(downloadResult.IsSuccess);
            Assert.True(tourManagerPage.IsOfflineAvailable(testTourId));
        }

        [Fact]
        public async Task App_Should_Handle_Network_Connectivity_Changes()
        {
            // Arrange
            var app = _fixture.Services.GetRequiredService<App>();
            
            // Act - Simulate network disconnection
            app.SimulateNetworkChange(false);
            
            // Assert
            Assert.True(app.IsOfflineMode);
            
            // Act - Simulate network reconnection
            app.SimulateNetworkChange(true);
            
            // Assert
            Assert.False(app.IsOfflineMode);
        }

        [Fact]
        public async Task App_Should_Persist_User_Preferences()
        {
            // Arrange
            var app = _fixture.Services.GetRequiredService<App>();
            var testLanguage = "vi";
            var testVolume = 0.8f;
            
            // Act
            await app.SetLanguageAsync(testLanguage);
            await app.SetVolumeAsync(testVolume);
            
            // Restart app simulation
            var newApp = _fixture.Services.GetRequiredService<App>();
            
            // Assert
            Assert.Equal(testLanguage, newApp.CurrentLanguage);
            Assert.Equal(testVolume, newApp.CurrentVolume);
        }

        [Fact]
        public async Task GPS_Location_Should_Trigger_Geofence_Events()
        {
            // Arrange
            var mapPage = new MapPage();
            var testLocation = new Location(10.123, 106.456);
            var geofenceTriggered = false;
            
            mapPage.GeofenceEntered += (sender, args) => geofenceTriggered = true;
            
            // Act
            await mapPage.UpdateLocationAsync(testLocation);
            
            // Assert
            Assert.True(geofenceTriggered);
        }

        [Fact]
        public async Task Audio_Should_Continue_Playing_In_Background()
        {
            // Arrange
            var audioPlayerPage = new AudioPlayerPage();
            var testAudioUrl = "https://api.example.com/audio/background-test.mp3";
            
            // Act
            await audioPlayerPage.LoadAudioAsync(testAudioUrl);
            await audioPlayerPage.PlayAsync();
            
            // Simulate app going to background
            audioPlayerPage.OnAppSleep();
            
            // Assert
            Assert.True(audioPlayerPage.IsPlaying);
            Assert.True(audioPlayerPage.IsBackgroundPlaybackEnabled);
        }
    }

    public class MauiAppFixture : IDisposable
    {
        public IServiceProvider Services { get; private set; }

        public MauiAppFixture()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>();
            
            // Configure test services
            builder.Services.AddSingleton<IConnectivity, MockConnectivity>();
            builder.Services.AddSingleton<IGeolocation, MockGeolocation>();
            builder.Services.AddSingleton<IMediaManager, MockMediaManager>();
            
            var app = builder.Build();
            Services = app.Services;
        }

        public void Dispose()
        {
            // Cleanup
        }
    }
}