using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace VinhKhanhAudioGuide.Backend.Tests.Integration
{
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GET_Tours_Should_Return_Tours_List()
        {
            var response = await _client.GetAsync("/api/tours");
            
            response.EnsureSuccessStatusCode();
            var tours = await response.Content.ReadFromJsonAsync<List<TourDto>>();
            Assert.NotNull(tours);
            Assert.True(tours.Count >= 0);
        }

        [Fact]
        public async Task POST_Tours_Should_Create_New_Tour()
        {
            var newTour = new CreateTourDto
            {
                Name = "Test Tour API",
                Description = "Created via API test",
                Language = "vi",
                Duration = TimeSpan.FromHours(2)
            };

            var response = await _client.PostAsJsonAsync("/api/tours", newTour);
            
            response.EnsureSuccessStatusCode();
            var createdTour = await response.Content.ReadFromJsonAsync<TourDto>();
            Assert.NotNull(createdTour);
            Assert.Equal(newTour.Name, createdTour.Name);
            Assert.True(createdTour.Id > 0);
        }

        [Fact]
        public async Task GET_POIs_Should_Return_POIs_List()
        {
            var response = await _client.GetAsync("/api/pois");
            
            response.EnsureSuccessStatusCode();
            var pois = await response.Content.ReadFromJsonAsync<List<PoiDto>>();
            Assert.NotNull(pois);
        }

        [Fact]
        public async Task POST_POI_Should_Create_New_POI()
        {
            var tourId = await CreateTestTour();
            var newPoi = new CreatePoiDto
            {
                Name = "Test POI API",
                Description = "Created via API test",
                Latitude = 10.123456,
                Longitude = 106.654321,
                TourId = tourId
            };

            var response = await _client.PostAsJsonAsync("/api/pois", newPoi);
            
            response.EnsureSuccessStatusCode();
            var createdPoi = await response.Content.ReadFromJsonAsync<PoiDto>();
            Assert.NotNull(createdPoi);
            Assert.Equal(newPoi.Name, createdPoi.Name);
            Assert.Equal(newPoi.Latitude, createdPoi.Latitude, 6);
        }

        [Fact]
        public async Task GET_POIs_By_Location_Should_Return_Nearby_POIs()
        {
            await CreateTestPoiWithLocation(10.123, 106.456);
            await CreateTestPoiWithLocation(10.124, 106.457);

            var response = await _client.GetAsync("/api/pois/nearby?latitude=10.123&longitude=106.456&radius=1000");
            
            response.EnsureSuccessStatusCode();
            var nearbyPois = await response.Content.ReadFromJsonAsync<List<PoiDto>>();
            Assert.NotNull(nearbyPois);
            Assert.True(nearbyPois.Count >= 2);
        }

        [Fact]
        public async Task POST_Analytics_Should_Track_User_Activity()
        {
            var analyticsData = new AnalyticsEventDto
            {
                EventType = "poi_visited",
                PoiId = await CreateTestPoi(),
                UserId = "test-user-123",
                Timestamp = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(5)
            };

            var response = await _client.PostAsJsonAsync("/api/analytics/events", analyticsData);
            
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task GET_Content_Sync_Should_Return_Latest_Content()
        {
            var response = await _client.GetAsync("/api/sync/content?lastSync=2024-01-01T00:00:00Z");
            
            response.EnsureSuccessStatusCode();
            var syncData = await response.Content.ReadFromJsonAsync<ContentSyncDto>();
            Assert.NotNull(syncData);
            Assert.NotNull(syncData.Tours);
            Assert.NotNull(syncData.Pois);
        }

        [Fact]
        public async Task API_Should_Handle_Invalid_Requests()
        {
            var invalidTour = new CreateTourDto { Name = "" };
            var response = await _client.PostAsJsonAsync("/api/tours", invalidTour);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

            var notFoundResponse = await _client.GetAsync("/api/tours/99999");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, notFoundResponse.StatusCode);
        }

        private async Task<int> CreateTestTour()
        {
            var tour = new CreateTourDto
            {
                Name = "Test Tour",
                Description = "Test Description",
                Language = "vi"
            };
            
            var response = await _client.PostAsJsonAsync("/api/tours", tour);
            var created = await response.Content.ReadFromJsonAsync<TourDto>();
            return created.Id;
        }

        private async Task<int> CreateTestPoi()
        {
            var tourId = await CreateTestTour();
            return await CreateTestPoiWithLocation(10.123, 106.456, tourId);
        }

        private async Task<int> CreateTestPoiWithLocation(double lat, double lng, int? tourId = null)
        {
            if (!tourId.HasValue)
                tourId = await CreateTestTour();
                
            var poi = new CreatePoiDto
            {
                Name = "Test POI",
                Description = "Test POI Description",
                Latitude = lat,
                Longitude = lng,
                TourId = tourId.Value
            };
            
            var response = await _client.PostAsJsonAsync("/api/pois", poi);
            var created = await response.Content.ReadFromJsonAsync<PoiDto>();
            return created.Id;
        }
    }
}