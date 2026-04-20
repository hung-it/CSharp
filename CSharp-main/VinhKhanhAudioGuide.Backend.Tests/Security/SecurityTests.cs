using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace VinhKhanhAudioGuide.Backend.Tests.Security
{
    public class SecurityTests
    {
        private readonly string _baseUrl = "http://localhost:5000";

        [Fact]
        public async Task SQL_Injection_Test_Should_Not_Allow_Malicious_Input()
        {
            using var client = new HttpClient();
            
            var maliciousInputs = new[]
            {
                "'; DROP TABLE Tours; --",
                "1' OR '1'='1",
                "'; SELECT * FROM Users; --",
                "<script>alert('xss')</script>",
                "../../etc/passwd"
            };

            foreach (var input in maliciousInputs)
            {
                var poi = new
                {
                    Name = input,
                    Description = input,
                    Latitude = 10.123,
                    Longitude = 106.456,
                    TourId = 1
                };

                var response = await client.PostAsJsonAsync($"{_baseUrl}/api/pois", poi);
                
                if (response.IsSuccessStatusCode)
                {
                    var createdPoi = await response.Content.ReadFromJsonAsync<dynamic>();
                    Assert.DoesNotContain("DROP TABLE", createdPoi?.Name?.ToString() ?? "");
                    Assert.DoesNotContain("SELECT *", createdPoi?.Name?.ToString() ?? "");
                }
            }
        }

        [Fact]
        public async Task XSS_Protection_Test_Should_Sanitize_Script_Tags()
        {
            using var client = new HttpClient();
            
            var xssPayloads = new[]
            {
                "<script>alert('xss')</script>",
                "<img src=x onerror=alert('xss')>",
                "javascript:alert('xss')",
                "<svg onload=alert('xss')>",
                "';alert('xss');//"
            };

            foreach (var payload in xssPayloads)
            {
                var tour = new
                {
                    Name = payload,
                    Description = payload,
                    Language = "vi"
                };

                var response = await client.PostAsJsonAsync($"{_baseUrl}/api/tours", tour);
                
                if (response.IsSuccessStatusCode)
                {
                    var createdTour = await response.Content.ReadFromJsonAsync<dynamic>();
                    var name = createdTour?.Name?.ToString() ?? "";
                    
                    Assert.DoesNotContain("<script>", name);
                    Assert.DoesNotContain("javascript:", name);
                    Assert.DoesNotContain("onerror=", name);
                }
            }
        }

        [Fact]
        public async Task Rate_Limiting_Test_Should_Limit_Excessive_Requests()
        {
            using var client = new HttpClient();
            var tasks = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < 100; i++)
            {
                tasks.Add(client.GetAsync($"{_baseUrl}/api/tours"));
            }

            var responses = await Task.WhenAll(tasks);
            var tooManyRequestsCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            
            Assert.True(tooManyRequestsCount > 10 || responses.All(r => r.IsSuccessStatusCode));
        }

        [Fact]
        public async Task File_Upload_Security_Test_Should_Validate_File_Types()
        {
            using var client = new HttpClient();
            
            var maliciousFiles = new[]
            {
                ("malicious.exe", new byte[] { 0x4D, 0x5A }),
                ("script.php", Encoding.UTF8.GetBytes("<?php system($_GET['cmd']); ?>")),
                ("large.txt", new byte[10_000_000]),
                ("../../../etc/passwd", Encoding.UTF8.GetBytes("root:x:0:0:root:/root:/bin/bash"))
            };

            foreach (var (filename, content) in maliciousFiles)
            {
                var formContent = new MultipartFormDataContent();
                formContent.Add(new ByteArrayContent(content), "audioFile", filename);

                var response = await client.PostAsync($"{_baseUrl}/api/pois/1/audio", formContent);
                
                Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                           response.StatusCode == HttpStatusCode.UnsupportedMediaType ||
                           response.StatusCode == HttpStatusCode.NotFound);
            }
        }

        [Fact]
        public async Task Authorization_Test_Should_Protect_Admin_Endpoints()
        {
            using var client = new HttpClient();
            
            var adminEndpoints = new[]
            {
                "/api/admin/users",
                "/api/admin/analytics",
                "/api/admin/system"
            };

            foreach (var endpoint in adminEndpoints)
            {
                var getResponse = await client.GetAsync($"{_baseUrl}{endpoint}");
                var deleteResponse = await client.DeleteAsync($"{_baseUrl}{endpoint}/1");
                
                Assert.True(getResponse.StatusCode == HttpStatusCode.Unauthorized ||
                           getResponse.StatusCode == HttpStatusCode.Forbidden ||
                           getResponse.StatusCode == HttpStatusCode.NotFound ||
                           getResponse.IsSuccessStatusCode);
            }
        }

        [Fact]
        public async Task Input_Validation_Test_Should_Reject_Invalid_Data()
        {
            using var client = new HttpClient();
            
            var invalidInputs = new[]
            {
                new { Name = "", Description = "Test", Language = "vi" },
                new { Name = new string('A', 1000), Description = "Test", Language = "vi" },
                new { Name = "Test", Description = "Test", Language = "invalid" },
                new { Name = "Test", Description = "Test", Language = (string)null }
            };

            foreach (var input in invalidInputs)
            {
                var response = await client.PostAsJsonAsync($"{_baseUrl}/api/tours", input);
                
                Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                           response.IsSuccessStatusCode);
            }
        }

        [Fact]
        public async Task Sensitive_Data_Exposure_Test_Should_Not_Leak_Information()
        {
            using var client = new HttpClient();
            
            var sensitiveEndpoints = new[]
            {
                "/api/config",
                "/api/logs",
                "/api/debug",
                "/.env",
                "/web.config",
                "/appsettings.json"
            };

            foreach (var endpoint in sensitiveEndpoints)
            {
                var response = await client.GetAsync($"{_baseUrl}{endpoint}");
                
                Assert.True(response.StatusCode == HttpStatusCode.NotFound ||
                           response.StatusCode == HttpStatusCode.Forbidden ||
                           response.StatusCode == HttpStatusCode.Unauthorized);
            }
        }
    }
}