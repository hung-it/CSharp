using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Application.Services;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Domain.Enums;
using VinhKhanhAudioGuide.Backend.Infrastructure;
using VinhKhanhAudioGuide.Backend.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var configuredDbPath = builder.Configuration["Backend:DatabasePath"] ?? "Data/vinh-khanh-guide.db";
var databasePath = Path.IsPathRooted(configuredDbPath)
    ? configuredDbPath
    : Path.Combine(builder.Environment.ContentRootPath, configuredDbPath);
var databaseDir = Path.GetDirectoryName(databasePath);
if (!string.IsNullOrWhiteSpace(databaseDir))
{
    Directory.CreateDirectory(databaseDir);
}

builder.Services.AddAudioGuideBackend(options => options.DatabasePath = databasePath);

const string CorsPolicyName = "FrontendClients";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, cors =>
    {
        if (allowedOrigins.Length == 0)
        {
            cors.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        cors.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(CorsPolicyName);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    await initializer.InitializeAsync();
}

var api = app.MapGroup("/api/v1");

api.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "VinhKhanhAudioGuide.Api",
    utc = DateTime.UtcNow
}));

api.MapPost("/users/resolve", async (
    [FromBody] ResolveUserRequest request,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.ExternalRef))
    {
        return Results.BadRequest(new { message = "externalRef is required." });
    }

    var externalRef = request.ExternalRef.Trim();
    var user = await dbContext.Users.FirstOrDefaultAsync(x => x.ExternalRef == externalRef, cancellationToken);

    if (user is null)
    {
        user = new User
        {
            ExternalRef = externalRef,
            PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage) ? "vi" : request.PreferredLanguage
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    return Results.Ok(new
    {
        user.Id,
        user.ExternalRef,
        user.PreferredLanguage,
        user.CreatedAtUtc
    });
});

api.MapGet("/users/{userId:guid}", async (Guid userId, AudioGuideDbContext dbContext, CancellationToken cancellationToken) =>
{
    var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    if (user is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        user.Id,
        user.ExternalRef,
        user.PreferredLanguage,
        user.CreatedAtUtc
    });
});

api.MapPost("/subscriptions/activate", async (
    [FromBody] ActivateSubscriptionRequest request,
    ISubscriptionService subscriptionService,
    CancellationToken cancellationToken) =>
{
    var planTier = Enum.TryParse<PlanTier>(request.PlanTier, true, out var parsed)
        ? parsed
        : request.PlanTier switch
        {
            "1" => PlanTier.Basic,
            "10" => PlanTier.PremiumSegmented,
            _ => throw new ArgumentException("Invalid planTier. Use Basic/PremiumSegmented or 1/10.")
        };

    var subscription = await subscriptionService.ActivateSubscriptionAsync(
        request.UserId,
        planTier,
        request.AmountUsd,
        cancellationToken);

    return Results.Ok(subscription);
});

api.MapGet("/subscriptions/users/{userId:guid}/active", async (
    Guid userId,
    ISubscriptionService subscriptionService,
    CancellationToken cancellationToken) =>
{
    var hasActive = await subscriptionService.HasActiveSubscriptionAsync(userId, cancellationToken);
    if (!hasActive)
    {
        return Results.Ok(new { hasActive = false });
    }

    var active = await subscriptionService.GetActiveSubscriptionAsync(userId, cancellationToken);
    return Results.Ok(new { hasActive = true, active });
});

api.MapGet("/subscriptions/users/{userId:guid}/access/{segmentCode}", async (
    Guid userId,
    string segmentCode,
    ISubscriptionService subscriptionService,
    CancellationToken cancellationToken) =>
{
    var hasAccess = await subscriptionService.HasAccessToSegmentAsync(userId, segmentCode, cancellationToken);
    return Results.Ok(new { userId, segmentCode, hasAccess });
});

api.MapGet("/pois", async (
    [FromQuery] string? district,
    IPoiService poiService,
    CancellationToken cancellationToken) =>
{
    var pois = string.IsNullOrWhiteSpace(district)
        ? await poiService.GetAllPoiAsync(cancellationToken)
        : await poiService.GetPoisByDistrictAsync(district, cancellationToken);

    return Results.Ok(pois.Select(MapPoiSummary));
});

api.MapGet("/pois/{poiId:guid}", async (Guid poiId, IPoiService poiService, CancellationToken cancellationToken) =>
{
    var poi = await poiService.GetPoiByIdAsync(poiId, cancellationToken);
    return poi is null ? Results.NotFound() : Results.Ok(MapPoiDetail(poi));
});

api.MapPost("/pois", async ([FromBody] CreatePoiRequest request, IPoiService poiService, CancellationToken cancellationToken) =>
{
    var poi = await poiService.CreatePoiAsync(
        request.Code,
        request.Name,
        request.Latitude,
        request.Longitude,
        request.TriggerRadiusMeters,
        request.Description,
        request.District,
        cancellationToken);

    return Results.Created($"/api/v1/pois/{poi.Id}", MapPoiDetail(poi));
});

api.MapPatch("/pois/{poiId:guid}", async (
    Guid poiId,
    [FromBody] UpdatePoiRequest request,
    IPoiService poiService,
    CancellationToken cancellationToken) =>
{
    var poi = await poiService.UpdatePoiAsync(
        poiId,
        request.Name,
        request.Description,
        request.TriggerRadiusMeters,
        cancellationToken);

    return Results.Ok(MapPoiDetail(poi));
});

api.MapGet("/pois/{poiId:guid}/audios", async (Guid poiId, IPoiService poiService, CancellationToken cancellationToken) =>
{
    var audios = await poiService.GetAudiosByPoiAsync(poiId, cancellationToken);
    return Results.Ok(audios.Select(MapAudioAsset));
});

api.MapPost("/pois/{poiId:guid}/audios", async (
    Guid poiId,
    [FromBody] AssignAudioRequest request,
    IPoiService poiService,
    CancellationToken cancellationToken) =>
{
    await poiService.AssignAudioAsync(
        poiId,
        request.LanguageCode,
        request.FilePath,
        request.DurationSeconds,
        request.IsTextToSpeech,
        cancellationToken);

    return Results.NoContent();
});

api.MapGet("/tours", async (ITourService tourService, CancellationToken cancellationToken) =>
{
    var tours = await tourService.GetAllToursAsync(cancellationToken);
    return Results.Ok(tours.Select(MapTourSummary));
});

api.MapGet("/tours/{tourId:guid}", async (Guid tourId, ITourService tourService, CancellationToken cancellationToken) =>
{
    var tour = await tourService.GetTourByIdAsync(tourId, cancellationToken);
    return tour is null ? Results.NotFound() : Results.Ok(MapTourDetail(tour));
});

api.MapGet("/tours/{tourId:guid}/stops", async (Guid tourId, ITourService tourService, CancellationToken cancellationToken) =>
{
    var stops = await tourService.GetTourStopsAsync(tourId, cancellationToken);
    return Results.Ok(stops.Select(MapTourStop));
});

api.MapPost("/tours", async ([FromBody] CreateTourRequest request, ITourService tourService, CancellationToken cancellationToken) =>
{
    var tour = await tourService.CreateTourAsync(request.Code, request.Name, request.Description, cancellationToken);
    return Results.Created($"/api/v1/tours/{tour.Id}", MapTourSummary(tour));
});

api.MapPost("/tours/{tourId:guid}/stops", async (
    Guid tourId,
    [FromBody] AddTourStopRequest request,
    ITourService tourService,
    CancellationToken cancellationToken) =>
{
    var stop = await tourService.AddStopAsync(
        tourId,
        request.PoiId,
        request.Sequence,
        request.NextStopHint,
        cancellationToken);

    return Results.Created($"/api/v1/tours/{tourId}/stops/{stop.Id}", MapTourStop(stop));
});

api.MapPost("/tours/{tourId:guid}/reorder", async (
    Guid tourId,
    [FromBody] ReorderTourStopsRequest request,
    ITourService tourService,
    CancellationToken cancellationToken) =>
{
    await tourService.ReorderStopsAsync(tourId, request.StopIdsInOrder, cancellationToken);
    return Results.NoContent();
});

api.MapPost("/sessions/start", async (
    [FromBody] StartSessionRequest request,
    IListeningSessionService sessionService,
    CancellationToken cancellationToken) =>
{
    var source = Enum.TryParse<TriggerSource>(request.TriggerSource, true, out var parsed)
        ? parsed
        : TriggerSource.Manual;

    var session = await sessionService.StartSessionAsync(
        request.UserId,
        request.PoiId,
        source,
        cancellationToken);

    return Results.Created($"/api/v1/sessions/{session.Id}", MapListeningSession(session));
});

api.MapPost("/sessions/{sessionId:guid}/end", async (
    Guid sessionId,
    [FromBody] EndSessionRequest request,
    IListeningSessionService sessionService,
    CancellationToken cancellationToken) =>
{
    var session = await sessionService.EndSessionAsync(sessionId, request.DurationSeconds, cancellationToken);
    return Results.Ok(MapListeningSession(session));
});

api.MapGet("/sessions/users/{userId:guid}", async (
    Guid userId,
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    IListeningSessionService sessionService,
    CancellationToken cancellationToken) =>
{
    var sessions = await sessionService.GetSessionsByUserAsync(userId, startDate, endDate, cancellationToken);
    return Results.Ok(sessions.Select(MapListeningSession));
});

api.MapPost("/qr/start", async (
    [FromBody] StartQrRequest request,
    IQrPlaybackService qrService,
    CancellationToken cancellationToken) =>
{
    var result = await qrService.StartSessionByQrAsync(
        request.UserId,
        request.QrPayload,
        request.LanguageCode,
        cancellationToken);

    return Results.Ok(new
    {
        session = MapListeningSession(result.Session),
        content = new
        {
            result.Content.PoiId,
            result.Content.PoiCode,
            result.Content.PoiName,
            result.Content.AudioPath,
            result.Content.IsTextToSpeech
        }
    });
});

api.MapGet("/analytics/top", async (
    [FromQuery] int limit,
    IAnalyticsService analytics,
    CancellationToken cancellationToken) =>
{
    var result = await analytics.GetTopPoisByListeningCountAsync(limit <= 0 ? 5 : limit, cancellationToken);
    return Results.Ok(result);
});

api.MapGet("/analytics/pois", async (IAnalyticsService analytics, CancellationToken cancellationToken) =>
{
    var result = await analytics.GetPoiListeningStatsAsync(cancellationToken);
    return Results.Ok(result);
});

api.MapGet("/analytics/usage", async (
    [FromQuery] int days,
    IAnalyticsService analytics,
    CancellationToken cancellationToken) =>
{
    var totalDays = days <= 0 ? 7 : days;
    var now = DateTime.UtcNow;
    var start = now.AddDays(-totalDays);
    var heatmap = await analytics.GetHeatmapDataAsync(start, now, 3, cancellationToken);
    var totalListens = heatmap.Sum(x => x.PointCount);

    return Results.Ok(new
    {
        days = totalDays,
        totalListens,
        activeCells = heatmap.Count(),
        startDateUtc = start,
        endDateUtc = now
    });
});

api.MapGet("/analytics/heatmap", async (
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    [FromQuery] int precision,
    IAnalyticsService analytics,
    CancellationToken cancellationToken) =>
{
    var result = await analytics.GetHeatmapDataAsync(startDate, endDate, precision <= 0 ? 3 : precision, cancellationToken);
    return Results.Ok(result);
});

api.MapPost("/geofence/evaluate", async (
    [FromBody] GeofenceEvaluateRequest request,
    IGeofenceService geofenceService,
    CancellationToken cancellationToken) =>
{
    var result = await geofenceService.EvaluateLocationAsync(
        request.UserId,
        request.Latitude,
        request.Longitude,
        request.NearFactor,
        cancellationToken);

    return Results.Ok(result);
});

api.MapPost("/narration/enqueue", async (
    [FromBody] EnqueueNarrationRequest request,
    INarrationQueueService queue,
    CancellationToken cancellationToken) =>
{
    var accepted = await queue.EnqueueAsync(
        request.UserId,
        request.PoiId,
        request.AudioPath,
        request.Priority,
        cancellationToken);

    return Results.Ok(new { accepted });
});

api.MapPost("/narration/next", async (
    [FromBody] UserScopedRequest request,
    INarrationQueueService queue,
    CancellationToken cancellationToken) =>
{
    var next = await queue.TryStartNextAsync(request.UserId, cancellationToken);
    return next is null ? Results.NoContent() : Results.Ok(next);
});

api.MapPost("/narration/complete", async (
    [FromBody] UserScopedRequest request,
    INarrationQueueService queue,
    CancellationToken cancellationToken) =>
{
    await queue.CompleteCurrentAsync(request.UserId, cancellationToken);
    return Results.NoContent();
});

api.MapPost("/sync/snapshot", async (
    [FromBody] ContentSyncSnapshot snapshot,
    IContentSyncService syncService,
    CancellationToken cancellationToken) =>
{
    var result = await syncService.SyncFromSnapshotAsync(snapshot, cancellationToken);
    return Results.Ok(result);
});

api.MapPut("/translations", async (
    [FromBody] UpsertTranslationRequest request,
    IContentTranslationService translationService,
    CancellationToken cancellationToken) =>
{
    var result = await translationService.UpsertTranslationAsync(
        request.ContentKey,
        request.LanguageCode,
        request.Value,
        cancellationToken);

    return Results.Ok(result);
});

api.MapGet("/translations/{contentKey}", async (
    string contentKey,
    [FromQuery] string languageCode,
    [FromQuery] string fallbackLanguageCode,
    IContentTranslationService translationService,
    CancellationToken cancellationToken) =>
{
    var value = await translationService.GetTranslationAsync(
        contentKey,
        string.IsNullOrWhiteSpace(languageCode) ? "vi" : languageCode,
        string.IsNullOrWhiteSpace(fallbackLanguageCode) ? "vi" : fallbackLanguageCode,
        cancellationToken);

    return value is null ? Results.NotFound() : Results.Ok(new { contentKey, value });
});

api.MapPost("/routes/anonymous/{anonymousRef}/points", async (
    string anonymousRef,
    [FromBody] LogRoutePointRequest request,
    IRouteTrackingService routeTracking,
    CancellationToken cancellationToken) =>
{
    var result = await routeTracking.LogAnonymousRoutePointAsync(
        anonymousRef,
        request.Latitude,
        request.Longitude,
        request.Source,
        cancellationToken);

    return Results.Created($"/api/v1/routes/anonymous/{anonymousRef}", MapRoutePoint(result));
});

api.MapGet("/routes/anonymous/{anonymousRef}", async (
    string anonymousRef,
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    IRouteTrackingService routeTracking,
    CancellationToken cancellationToken) =>
{
    var points = await routeTracking.GetAnonymousRouteAsync(anonymousRef, startDate, endDate, cancellationToken);
    return Results.Ok(points.Select(MapRoutePoint));
});

object MapPoiSummary(Poi poi) => new
{
    poi.Id,
    poi.Code,
    poi.Name,
    poi.Description,
    poi.Latitude,
    poi.Longitude,
    poi.TriggerRadiusMeters,
    poi.District
};

object MapPoiDetail(Poi poi) => new
{
    poi.Id,
    poi.Code,
    poi.Name,
    poi.Description,
    poi.Latitude,
    poi.Longitude,
    poi.TriggerRadiusMeters,
    poi.District,
    audioAssets = poi.AudioAssets.Select(MapAudioAsset)
};

object MapAudioAsset(AudioAsset audio) => new
{
    audio.Id,
    audio.PoiId,
    audio.LanguageCode,
    audio.FilePath,
    audio.DurationSeconds,
    audio.IsTextToSpeech
};

object MapTourSummary(Tour tour) => new
{
    tour.Id,
    tour.Code,
    tour.Name,
    tour.Description
};

object MapTourDetail(Tour tour) => new
{
    tour.Id,
    tour.Code,
    tour.Name,
    tour.Description,
    stops = tour.Stops.OrderBy(x => x.Sequence).Select(MapTourStop)
};

object MapTourStop(TourStop stop) => new
{
    stop.Id,
    stop.TourId,
    stop.PoiId,
    stop.Sequence,
    stop.NextStopHint,
    poi = stop.Poi is null ? null : MapPoiSummary(stop.Poi)
};

object MapListeningSession(ListeningSession session) => new
{
    session.Id,
    session.UserId,
    session.PoiId,
    session.StartedAtUtc,
    session.EndedAtUtc,
    session.DurationSeconds,
    triggerSource = session.TriggerSource.ToString()
};

object MapRoutePoint(RoutePoint point) => new
{
    point.Id,
    point.UserId,
    point.RecordedAtUtc,
    point.Latitude,
    point.Longitude,
    point.Source
};

app.Run();

public sealed record CreatePoiRequest(
    string Code,
    string Name,
    double Latitude,
    double Longitude,
    double TriggerRadiusMeters = 30,
    string? Description = null,
    string? District = null);

public sealed record UpdatePoiRequest(
    string? Name,
    string? Description,
    double? TriggerRadiusMeters);

public sealed record AssignAudioRequest(
    string LanguageCode,
    string FilePath,
    int DurationSeconds,
    bool IsTextToSpeech = false);

public sealed record CreateTourRequest(
    string Code,
    string Name,
    string? Description = null);

public sealed record AddTourStopRequest(
    Guid PoiId,
    int Sequence,
    string? NextStopHint = null);

public sealed record ReorderTourStopsRequest(List<Guid> StopIdsInOrder);

public sealed record StartSessionRequest(
    Guid UserId,
    Guid PoiId,
    string TriggerSource = "Manual");

public sealed record EndSessionRequest(int DurationSeconds);

public sealed record StartQrRequest(
    Guid UserId,
    string QrPayload,
    string LanguageCode = "vi");

public sealed record GeofenceEvaluateRequest(
    Guid UserId,
    double Latitude,
    double Longitude,
    double NearFactor = 1.5);

public sealed record EnqueueNarrationRequest(
    Guid UserId,
    Guid PoiId,
    string AudioPath,
    int Priority = 0);

public sealed record UserScopedRequest(Guid UserId);

public sealed record UpsertTranslationRequest(
    string ContentKey,
    string LanguageCode,
    string Value);

public sealed record LogRoutePointRequest(
    double Latitude,
    double Longitude,
    string Source = "gps");

public sealed record ResolveUserRequest(
    string ExternalRef,
    string PreferredLanguage = "vi");

public sealed record ActivateSubscriptionRequest(
    Guid UserId,
    string PlanTier,
    decimal AmountUsd);
