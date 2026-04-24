using System.Net.Http.Json;

namespace VinhKhanhAudioGuide.App;

public static class TrackingService
{
    private static readonly HttpClient _httpClient = AppConfig.CreateHttpClient();
    private static string _anonymousRef = $"ANON_{Guid.NewGuid():N}";
    private static Guid? _currentVisitId;
    private static Guid? _currentTourViewId;
    private static int _sessionCount = 0;
    private static int _totalListenSeconds = 0;
    private static DateTime _sessionStartTime = DateTime.MinValue;

    public static string AnonymousRef => _anonymousRef;

    // ========== User Resolution ==========

    public static async Task<string> ResolveUserAsync()
    {
        try
        {
            return await AppConfig.ResolveDefaultUserIdAsync(_httpClient);
        }
        catch { /* Silent */ }

        return _anonymousRef;
    }

    // ========== Visit Tracking ==========

    public static async Task<Guid?> StartVisitAsync(
        Guid userId,
        Guid poiId,
        string triggerSource = "Map",
        string pageSource = "Map",
        double? latitude = null,
        double? longitude = null)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("visits/start", new
            {
                UserId = userId,
                PoiId = poiId,
                TriggerSource = triggerSource,
                PageSource = pageSource,
                Latitude = latitude,
                Longitude = longitude,
                AnonymousRef = _anonymousRef
            });

            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<VisitStartResult>();
            _currentVisitId = result?.Id;
            
            System.Diagnostics.Debug.WriteLine($"[Tracking] Started visit for POI: {poiId}");
            return _currentVisitId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tracking] StartVisit error: {ex.Message}");
            return null;
        }
    }

    public static async Task EndVisitAsync()
    {
        if (_currentVisitId == null) return;

        try
        {
            // Update audio data first
            if (_sessionCount > 0)
            {
                await _httpClient.PostAsJsonAsync($"visits/{_currentVisitId}/audio", new
                {
                    ListeningSessionCount = _sessionCount,
                    TotalListenDurationSeconds = _totalListenSeconds
                });
            }

            // End the visit
            await _httpClient.PostAsJsonAsync($"visits/{_currentVisitId}/end", new { });

            System.Diagnostics.Debug.WriteLine($"[Tracking] Ended visit {_currentVisitId}: {_sessionCount} sessions, {_totalListenSeconds}s");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tracking] EndVisit error: {ex.Message}");
        }

        _currentVisitId = null;
        _sessionCount = 0;
        _totalListenSeconds = 0;
    }

    // ========== Listening Session Tracking ==========

    public static void RecordListeningStart()
    {
        _sessionCount++;
        _sessionStartTime = DateTime.UtcNow;
        System.Diagnostics.Debug.WriteLine($"[Tracking] Listening started. Total sessions: {_sessionCount}");
    }

    public static void RecordListeningEnd(int durationSeconds)
    {
        _totalListenSeconds += durationSeconds;
        System.Diagnostics.Debug.WriteLine($"[Tracking] Listening ended. Duration: {durationSeconds}s. Total: {_totalListenSeconds}s");
    }

    // ========== Tour View Tracking ==========

    public static async Task<Guid?> StartTourViewAsync(Guid userId, Guid tourId)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"tours/{tourId}/view/start", new
            {
                UserId = userId,
                AnonymousRef = _anonymousRef
            });

            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<TourViewStartResult>();
            _currentTourViewId = result?.Id;
            
            System.Diagnostics.Debug.WriteLine($"[Tracking] Started tour view: {tourId}");
            return _currentTourViewId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tracking] StartTourView error: {ex.Message}");
            return null;
        }
    }

    public static async Task EndTourViewAsync(int poiVisitedCount, int audioListenedCount)
    {
        if (_currentTourViewId == null) return;

        try
        {
            await _httpClient.PostAsJsonAsync($"tours/{_currentTourViewId}/view/{_currentTourViewId}/end", new
            {
                PoiVisitedCount = poiVisitedCount,
                AudioListenedCount = audioListenedCount
            });

            System.Diagnostics.Debug.WriteLine($"[Tracking] Ended tour view: {poiVisitedCount} POIs, {audioListenedCount} audio");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tracking] EndTourView error: {ex.Message}");
        }

        _currentTourViewId = null;
    }

    // ========== Geofence Tracking ==========

    public static async Task RecordGeofenceEventAsync(
        Guid userId,
        Guid poiId,
        string eventType,
        double latitude,
        double longitude,
        double distanceFromCenterMeters)
    {
        try
        {
            await _httpClient.PostAsJsonAsync("geofence/events", new
            {
                UserId = userId,
                PoiId = poiId,
                EventType = eventType,
                Latitude = latitude,
                Longitude = longitude,
                DistanceFromCenterMeters = distanceFromCenterMeters,
                AnonymousRef = _anonymousRef
            });

            System.Diagnostics.Debug.WriteLine($"[Tracking] Geofence event: {eventType} at {poiId}, distance: {distanceFromCenterMeters}m");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tracking] Geofence event error: {ex.Message}");
        }
    }

    // ========== Route Tracking ==========

    public static async Task LogRoutePointAsync(double latitude, double longitude, string source = "gps")
    {
        try
        {
            await _httpClient.PostAsJsonAsync($"routes/anonymous/{_anonymousRef}/points", new
            {
                Latitude = latitude,
                Longitude = longitude,
                Source = source
            });

            System.Diagnostics.Debug.WriteLine($"[Tracking] Route point logged: {latitude}, {longitude}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tracking] Route point error: {ex.Message}");
        }
    }

    // ========== Analytics Sync ==========

    public static async Task<DashboardStats> GetDashboardStatsAsync()
    {
        try
        {
            var stats = new DashboardStats();

            // Get POI count
            var poisResponse = await _httpClient.GetAsync("pois");
            if (poisResponse.IsSuccessStatusCode)
            {
                var pois = await poisResponse.Content.ReadFromJsonAsync<List<object>>();
                stats.TotalPois = pois?.Count ?? 0;
            }

            // Get tour count
            var toursResponse = await _httpClient.GetAsync("tours");
            if (toursResponse.IsSuccessStatusCode)
            {
                var tours = await toursResponse.Content.ReadFromJsonAsync<List<object>>();
                stats.TotalTours = tours?.Count ?? 0;
            }

            // Get usage stats
            var usageResponse = await _httpClient.GetFromJsonAsync<UsageStatsResult>("analytics/usage?days=7");
            if (usageResponse != null)
            {
                stats.WeeklyListens = usageResponse.TotalListens;
                stats.WeeklyActivePois = usageResponse.ActiveCells;
            }

            // Get top POIs
            var topResponse = await _httpClient.GetFromJsonAsync<List<TopPoiResult>>("analytics/top?limit=5");
            if (topResponse != null)
            {
                stats.TopPois = topResponse;
            }

            return stats;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tracking] GetDashboardStats error: {ex.Message}");
            return new DashboardStats { TotalPois = 14, TotalTours = 2 };
        }
    }

    public static async Task<MyActivityStats> GetMyActivityStatsAsync(string userId)
    {
        var stats = new MyActivityStats();

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            return stats;

        try
        {
            var sessions = await _httpClient.GetFromJsonAsync<List<SessionResult>>($"sessions/users/{userId}");
            if (sessions != null)
            {
                stats.TotalListens = sessions.Count;
                stats.UniquePoisVisited = sessions.Select(s => s.PoiId).Distinct().Count();
                stats.TotalListenSeconds = sessions
                    .Where(s => s.DurationSeconds.HasValue)
                    .Sum(s => s.DurationSeconds!.Value);
            }
        }
        catch { /* Silent */ }

        return stats;
    }
}

// ========== Response Models ==========

public class ResolveResult
{
    public bool Success { get; set; }
    public Guid Id { get; set; }
    public string? Username { get; set; }
}

public class VisitStartResult
{
    public Guid Id { get; set; }
}

public class TourViewStartResult
{
    public Guid Id { get; set; }
}

public class UsageStatsResult
{
    public int Days { get; set; }
    public int TotalListens { get; set; }
    public int ActiveCells { get; set; }
}

public class TopPoiResult
{
    // API returns camelCase properties
    public string? poiName { get; set; }
    public string? district { get; set; }
    public int listeningCount { get; set; }
    public string PoiName => poiName ?? string.Empty;
    public string District => district ?? string.Empty;
    public int ListenCount => listeningCount;
}

public class SessionResult
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PoiId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public int? DurationSeconds { get; set; }
}

// ========== Stats Models ==========

public class DashboardStats
{
    public int TotalPois { get; set; }
    public int TotalTours { get; set; }
    public int WeeklyListens { get; set; }
    public int WeeklyActivePois { get; set; }
    public List<TopPoiResult> TopPois { get; set; } = new();
}

public class MyActivityStats
{
    public int TotalListens { get; set; }
    public int UniquePoisVisited { get; set; }
    public int TotalListenSeconds { get; set; }
}
