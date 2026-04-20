using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using Xunit;

namespace VinhKhanhAudioGuide.App.Tests
{
    public class MobileUITests : IDisposable
    {
        private AppiumDriver _driver;
        private readonly bool _isAndroid;

        public MobileUITests()
        {
            _isAndroid = Environment.GetEnvironmentVariable("PLATFORM") == "Android";
            InitializeDriver();
        }

        private void InitializeDriver()
        {
            var options = new AppiumOptions();
            
            if (_isAndroid)
            {
                options.AddAdditionalCapability("platformName", "Android");
                options.AddAdditionalCapability("deviceName", "Android Emulator");
                options.AddAdditionalCapability("app", "path/to/VinhKhanhAudioGuide.apk");
                options.AddAdditionalCapability("automationName", "UiAutomator2");
                _driver = new AndroidDriver(new Uri("http://localhost:4723/wd/hub"), options);
            }
            else
            {
                options.AddAdditionalCapability("platformName", "iOS");
                options.AddAdditionalCapability("deviceName", "iPhone Simulator");
                options.AddAdditionalCapability("app", "path/to/VinhKhanhAudioGuide.app");
                options.AddAdditionalCapability("automationName", "XCUITest");
                _driver = new IOSDriver(new Uri("http://localhost:4723/wd/hub"), options);
            }
        }

        [Fact]
        public void App_Should_Launch_Successfully()
        {
            // Assert
            Assert.NotNull(_driver);
            Assert.True(_driver.Context.Contains("NATIVE"));
        }

        [Fact]
        public void MainPage_Should_Display_Welcome_Screen()
        {
            // Arrange & Act
            var welcomeText = _driver.FindElement(By.Id("WelcomeText"));
            var startButton = _driver.FindElement(By.Id("StartButton"));
            
            // Assert
            Assert.True(welcomeText.Displayed);
            Assert.True(startButton.Displayed);
            Assert.Contains("Chào mừng", welcomeText.Text);
        }

        [Fact]
        public void QR_Scanner_Should_Open_Camera()
        {
            // Arrange
            var scanQrButton = _driver.FindElement(By.Id("ScanQrButton"));
            
            // Act
            scanQrButton.Click();
            Thread.Sleep(2000); // Wait for camera to initialize
            
            // Assert
            var cameraView = _driver.FindElement(By.Id("CameraView"));
            Assert.True(cameraView.Displayed);
        }

        [Fact]
        public void Map_Should_Display_User_Location()
        {
            // Arrange
            var mapButton = _driver.FindElement(By.Id("MapButton"));
            
            // Act
            mapButton.Click();
            Thread.Sleep(3000); // Wait for map to load
            
            // Assert
            var mapView = _driver.FindElement(By.Id("MapView"));
            var userLocationMarker = _driver.FindElement(By.Id("UserLocationMarker"));
            
            Assert.True(mapView.Displayed);
            Assert.True(userLocationMarker.Displayed);
        }

        [Fact]
        public void Audio_Player_Should_Control_Playback()
        {
            // Arrange
            NavigateToAudioPlayer();
            
            // Act & Assert - Play button
            var playButton = _driver.FindElement(By.Id("PlayButton"));
            playButton.Click();
            Thread.Sleep(1000);
            
            var pauseButton = _driver.FindElement(By.Id("PauseButton"));
            Assert.True(pauseButton.Displayed);
            
            // Act & Assert - Pause button
            pauseButton.Click();
            Thread.Sleep(1000);
            
            playButton = _driver.FindElement(By.Id("PlayButton"));
            Assert.True(playButton.Displayed);
        }

        [Fact]
        public void Tour_List_Should_Display_Available_Tours()
        {
            // Arrange
            var toursButton = _driver.FindElement(By.Id("ToursButton"));
            
            // Act
            toursButton.Click();
            Thread.Sleep(2000);
            
            // Assert
            var toursList = _driver.FindElements(By.ClassName("TourItem"));
            Assert.True(toursList.Count > 0);
            
            var firstTour = toursList.First();
            Assert.True(firstTour.Displayed);
            Assert.True(firstTour.FindElement(By.ClassName("TourName")).Displayed);
        }

        [Fact]
        public void POI_Detail_Should_Show_Information()
        {
            // Arrange
            NavigateToFirstPOI();
            
            // Assert
            var poiName = _driver.FindElement(By.Id("PoiName"));
            var poiDescription = _driver.FindElement(By.Id("PoiDescription"));
            var playAudioButton = _driver.FindElement(By.Id("PlayAudioButton"));
            
            Assert.True(poiName.Displayed);
            Assert.True(poiDescription.Displayed);
            Assert.True(playAudioButton.Displayed);
            Assert.False(string.IsNullOrEmpty(poiName.Text));
        }

        [Fact]
        public void Settings_Should_Change_Language()
        {
            // Arrange
            var settingsButton = _driver.FindElement(By.Id("SettingsButton"));
            settingsButton.Click();
            Thread.Sleep(1000);
            
            // Act
            var languageDropdown = _driver.FindElement(By.Id("LanguageDropdown"));
            languageDropdown.Click();
            
            var englishOption = _driver.FindElement(By.XPath("//option[@value='en']"));
            englishOption.Click();
            
            var saveButton = _driver.FindElement(By.Id("SaveSettingsButton"));
            saveButton.Click();
            
            // Assert
            Thread.Sleep(1000);
            var welcomeText = _driver.FindElement(By.Id("WelcomeText"));
            Assert.Contains("Welcome", welcomeText.Text);
        }

        [Fact]
        public void Offline_Mode_Should_Work_Without_Internet()
        {
            // Arrange
            ToggleAirplaneMode(true);
            
            // Act
            var offlineTourButton = _driver.FindElement(By.Id("OfflineToursButton"));
            offlineTourButton.Click();
            Thread.Sleep(2000);
            
            // Assert
            var offlineToursList = _driver.FindElements(By.ClassName("OfflineTourItem"));
            Assert.True(offlineToursList.Count > 0);
            
            // Cleanup
            ToggleAirplaneMode(false);
        }

        [Fact]
        public void GPS_Should_Trigger_POI_Notifications()
        {
            // Arrange
            SetMockLocation(10.123, 106.456); // Near a test POI
            
            // Act
            Thread.Sleep(5000); // Wait for geofence detection
            
            // Assert
            var notification = _driver.FindElement(By.Id("PoiNotification"));
            Assert.True(notification.Displayed);
            Assert.Contains("Bạn đang gần", notification.Text);
        }

        [Fact]
        public void Volume_Control_Should_Adjust_Audio()
        {
            // Arrange
            NavigateToAudioPlayer();
            var playButton = _driver.FindElement(By.Id("PlayButton"));
            playButton.Click();
            Thread.Sleep(2000);
            
            // Act
            var volumeSlider = _driver.FindElement(By.Id("VolumeSlider"));
            volumeSlider.SendKeys("50"); // Set to 50%
            
            // Assert
            var volumeDisplay = _driver.FindElement(By.Id("VolumeDisplay"));
            Assert.Contains("50%", volumeDisplay.Text);
        }

        [Fact]
        public void Search_Should_Filter_POIs()
        {
            // Arrange
            var searchButton = _driver.FindElement(By.Id("SearchButton"));
            searchButton.Click();
            
            // Act
            var searchInput = _driver.FindElement(By.Id("SearchInput"));
            searchInput.SendKeys("Temple");
            
            var searchSubmitButton = _driver.FindElement(By.Id("SearchSubmitButton"));
            searchSubmitButton.Click();
            Thread.Sleep(2000);
            
            // Assert
            var searchResults = _driver.FindElements(By.ClassName("SearchResultItem"));
            Assert.True(searchResults.Count > 0);
            Assert.All(searchResults, result => 
                Assert.Contains("Temple", result.FindElement(By.ClassName("ResultName")).Text));
        }

        private void NavigateToAudioPlayer()
        {
            var toursButton = _driver.FindElement(By.Id("ToursButton"));
            toursButton.Click();
            Thread.Sleep(1000);
            
            var firstTour = _driver.FindElement(By.ClassName("TourItem"));
            firstTour.Click();
            Thread.Sleep(1000);
            
            var firstPoi = _driver.FindElement(By.ClassName("PoiItem"));
            firstPoi.Click();
            Thread.Sleep(1000);
            
            var playAudioButton = _driver.FindElement(By.Id("PlayAudioButton"));
            playAudioButton.Click();
            Thread.Sleep(2000);
        }

        private void NavigateToFirstPOI()
        {
            var toursButton = _driver.FindElement(By.Id("ToursButton"));
            toursButton.Click();
            Thread.Sleep(1000);
            
            var firstTour = _driver.FindElement(By.ClassName("TourItem"));
            firstTour.Click();
            Thread.Sleep(1000);
            
            var firstPoi = _driver.FindElement(By.ClassName("PoiItem"));
            firstPoi.Click();
            Thread.Sleep(2000);
        }

        private void ToggleAirplaneMode(bool enable)
        {
            if (_isAndroid)
            {
                ((AndroidDriver)_driver).ToggleAirplaneMode();
            }
            else
            {
                // iOS simulator airplane mode toggle
                ((IOSDriver)_driver).ExecuteScript("mobile: setConnectivity", 
                    new Dictionary<string, object> { { "wifi", !enable }, { "data", !enable } });
            }
        }

        private void SetMockLocation(double latitude, double longitude)
        {
            if (_isAndroid)
            {
                ((AndroidDriver)_driver).SetLocation(new Location(latitude, longitude, 0));
            }
            else
            {
                ((IOSDriver)_driver).ExecuteScript("mobile: setLocation", 
                    new Dictionary<string, object> 
                    { 
                        { "latitude", latitude }, 
                        { "longitude", longitude } 
                    });
            }
        }

        public void Dispose()
        {
            _driver?.Quit();
        }
    }
}