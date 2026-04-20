using System.Diagnostics;
using System.Net.Http.Json;
using Xunit;

namespace VinhKhanhAudioGuide.Backend.Tests.Performance
{
    public class ApiPerformanceTests
    {
        private readonly string _baseUrl = "http://localhost:5000";

        [Fact]
        public async Task Load_Test_Get_Tours_Should_Handle_Concurrent_Requests()
        {
            var tasks = new List<Task<(bool Success, long ElapsedMs)>>();
            var stopwatch = Stopwatch.StartNew();

            // Tạo 50 request đồng thời
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(MeasureApiCall($"{_baseUrl}/api/tours"));
            }

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            var successCount = results.Count(r => r.Success);
            var avgResponseTime = results.Where(r => r.Success).Average(r => r.ElapsedMs);

            Assert.True(successCount >= 45); // 90% success rate
            Assert.True(avgResponseTime < 2000); // < 2s average response time
            Assert.True(stopwatch.ElapsedMilliseconds < 10000); // Complete within 10s
        }

        [Fact]
        public async Task Stress_Test_POI_Creation_Should_Maintain_Performance()
        {
            var tasks = new List<Task<bool>>();

            for (int i = 0; i < 20; i++)
            {
                tasks.Add(CreateTestPoi(i));
            }

            var results = await Task.WhenAll(tasks);
            var successCount = results.Count(r => r);

            Assert.True(successCount >= 18); // 90% success rate
        }

        [Fact]
        public async Task Memory_Usage_Test_Large_Dataset_Should_Not_Exceed_Limits()
        {
            using var client = new HttpClient();
            var initialMemory = GC.GetTotalMemory(false);

            // Request large dataset multiple times
            for (int i = 0; i < 10; i++)
            {
                var response = await client.GetAsync($"{_baseUrl}/api/pois?pageSize=1000");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Assert.True(content.Length > 0);
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            Assert.True(memoryIncrease < 50_000_000); // < 50MB memory increase
        }

        [Fact]
        public async Task Database_Connection_Pool_Test_Should_Handle_Concurrent_Connections()
        {
            var readTasks = new List<Task<bool>>();

            // Tạo nhiều connection đồng thời
            for (int i = 0; i < 30; i++)
            {
                readTasks.Add(TestDatabaseRead("tours"));
                readTasks.Add(TestDatabaseRead("pois"));
                readTasks.Add(TestDatabaseRead("analytics/stats"));
            }

            var results = await Task.WhenAll(readTasks);
            var successCount = results.Count(r => r);

            Assert.True(successCount >= 80); // 90% success rate
        }

        [Fact]
        public async Task Response_Time_Test_Critical_Endpoints_Should_Be_Fast()
        {
            var endpoints = new[]
            {
                "/api/tours",
                "/api/pois",
                "/api/pois/nearby?latitude=10.123&longitude=106.456&radius=1000",
                "/api/sync/content"
            };

            foreach (var endpoint in endpoints)
            {
                var (success, elapsedMs) = await MeasureApiCall($"{_baseUrl}{endpoint}");
                
                Assert.True(success, $"Endpoint {endpoint} failed");
                Assert.True(elapsedMs < 1000, $"Endpoint {endpoint} took {elapsedMs}ms (should be < 1000ms)");
            }
        }

        private async Task<(bool Success, long ElapsedMs)> MeasureApiCall(string url)
        {
            try
            {
                using var client = new HttpClient();
                var stopwatch = Stopwatch.StartNew();
                
                var response = await client.GetAsync(url);
                stopwatch.Stop();
                
                return (response.IsSuccessStatusCode, stopwatch.ElapsedMilliseconds);
            }
            catch
            {
                return (false, 0);
            }
        }

        private async Task<bool> CreateTestPoi(int index)
        {
            try
            {
                using var client = new HttpClient();
                var poi = new
                {
                    Name = $"Performance Test POI {index}",
                    Description = $"Created for performance testing {index}",
                    Latitude = 10.123 + (index * 0.001),
                    Longitude = 106.456 + (index * 0.001),
                    TourId = 1
                };

                var response = await client.PostAsJsonAsync($"{_baseUrl}/api/pois", poi);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestDatabaseRead(string endpoint)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync($"{_baseUrl}/api/{endpoint}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}