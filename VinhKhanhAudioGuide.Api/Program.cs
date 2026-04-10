using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
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

        var normalizedAllowedOrigins = allowedOrigins
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        cors.SetIsOriginAllowed(origin =>
        {
            if (normalizedAllowedOrigins.Contains(origin))
            {
                return true;
            }

            // In local development, allow localhost/127.0.0.1 on any port (e.g. Vite 5173/5174).
            if (builder.Environment.IsDevelopment() && Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            {
                return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        })
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(CorsPolicyName);

var uploadsRootPath = Path.Combine(app.Environment.ContentRootPath, "Data", "uploads");
Directory.CreateDirectory(uploadsRootPath);
Directory.CreateDirectory(Path.Combine(uploadsRootPath, "audio"));

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRootPath),
    RequestPath = "/media"
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    await initializer.InitializeAsync();
}

var api = app.MapGroup("/api/v1")
    .AddEndpointFilter<DomainExceptionFilter>();

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

api.MapGet("/users", async (
    [FromQuery] string? search,
    [FromQuery] int limit,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var safeLimit = limit <= 0 ? 20 : Math.Min(limit, 200);
    var query = dbContext.Users.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var keyword = search.Trim();
        query = query.Where(x => x.ExternalRef.Contains(keyword));
    }

    var users = await query
        .OrderByDescending(x => x.CreatedAtUtc)
        .Take(safeLimit)
        .Select(x => new
        {
            x.Id,
            x.ExternalRef,
            x.PreferredLanguage,
            x.CreatedAtUtc
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(users);
});

api.MapDelete("/users/{userId:guid}", async (
    Guid userId,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    if (user is null)
    {
        return Results.NoContent();
    }

    var sessions = await dbContext.ListeningSessions
        .Where(x => x.UserId == userId)
        .ToListAsync(cancellationToken);

    var routePoints = await dbContext.RoutePoints
        .Where(x => x.UserId == userId)
        .ToListAsync(cancellationToken);

    var subscriptions = await dbContext.Subscriptions
        .Where(x => x.UserId == userId)
        .ToListAsync(cancellationToken);

    var entitlements = await dbContext.UserEntitlements
        .Where(x => x.UserId == userId)
        .ToListAsync(cancellationToken);

    dbContext.ListeningSessions.RemoveRange(sessions);
    dbContext.RoutePoints.RemoveRange(routePoints);
    dbContext.Subscriptions.RemoveRange(subscriptions);
    dbContext.UserEntitlements.RemoveRange(entitlements);
    dbContext.Users.Remove(user);

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});

api.MapPost("/users/demo-seed", async (
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var demoUsers = new[]
    {
        (ExternalRef: "DEMO_ADMIN_01", PreferredLanguage: "vi"),
        (ExternalRef: "DEMO_BASIC_01", PreferredLanguage: "vi"),
        (ExternalRef: "DEMO_PREMIUM_01", PreferredLanguage: "vi"),
        (ExternalRef: "DEMO_EN_01", PreferredLanguage: "en")
    };

    var createdCount = 0;
    foreach (var demo in demoUsers)
    {
        var exists = await dbContext.Users.AnyAsync(x => x.ExternalRef == demo.ExternalRef, cancellationToken);
        if (exists)
        {
            continue;
        }

        dbContext.Users.Add(new User
        {
            ExternalRef = demo.ExternalRef,
            PreferredLanguage = demo.PreferredLanguage
        });

        createdCount++;
    }

    if (createdCount > 0)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    var users = await dbContext.Users
        .AsNoTracking()
        .Where(x => x.ExternalRef.StartsWith("DEMO_"))
        .OrderBy(x => x.ExternalRef)
        .Select(x => new
        {
            x.Id,
            x.ExternalRef,
            x.PreferredLanguage,
            x.CreatedAtUtc
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(new
    {
        createdCount,
        users
    });
});

api.MapGet("/feature-segments", async (AudioGuideDbContext dbContext, CancellationToken cancellationToken) =>
{
    var segments = await dbContext.FeatureSegments
        .AsNoTracking()
        .OrderBy(x => x.Code)
        .Select(x => new
        {
            x.Id,
            x.Code,
            x.Name,
            x.Description
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(segments);
});

api.MapPost("/subscriptions/activate", async (
    [FromBody] ActivateSubscriptionRequest request,
    ISubscriptionService subscriptionService,
    CancellationToken cancellationToken) =>
{
    var planTier = ParsePlanTier(request.PlanTier);

    var subscription = await subscriptionService.ActivateSubscriptionAsync(
        request.UserId,
        planTier,
        request.AmountUsd,
        cancellationToken);

    return Results.Ok(MapSubscription(subscription));
});

api.MapPost("/subscriptions", async (
    [FromBody] CreateSubscriptionRequest request,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var userExists = await dbContext.Users.AnyAsync(x => x.Id == request.UserId, cancellationToken);
    if (!userExists)
    {
        throw new KeyNotFoundException($"User with ID {request.UserId} not found.");
    }

    var planTier = ParsePlanTier(request.PlanTier);
    var isActive = request.IsActive;

    if (isActive)
    {
        var activeSubscriptions = await dbContext.Subscriptions
            .Where(x => x.UserId == request.UserId && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var item in activeSubscriptions)
        {
            item.IsActive = false;
        }
    }

    var subscription = new Subscription
    {
        UserId = request.UserId,
        PlanTier = planTier,
        AmountUsd = request.AmountUsd,
        IsActive = isActive,
        ExpiresAtUtc = request.ExpiresAtUtc,
        ActivatedAtUtc = DateTime.UtcNow
    };

    dbContext.Subscriptions.Add(subscription);
    await dbContext.SaveChangesAsync(cancellationToken);

    var hasAnyActivePremium = await dbContext.Subscriptions
        .AsNoTracking()
        .AnyAsync(x => x.UserId == request.UserId && x.IsActive && x.PlanTier == PlanTier.PremiumSegmented, cancellationToken);

    if (hasAnyActivePremium)
    {
        await EnsurePremiumEntitlementsAsync(dbContext, request.UserId, cancellationToken);
    }
    else
    {
        await RevokePremiumEntitlementsAsync(dbContext, request.UserId, cancellationToken);
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/v1/subscriptions/{subscription.Id}", MapSubscription(subscription));
});

api.MapGet("/subscriptions/{subscriptionId:guid}", async (
    Guid subscriptionId,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var item = await dbContext.Subscriptions
        .AsNoTracking()
        .Include(x => x.User)
        .FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

    if (item is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(MapSubscription(item));
});

api.MapPatch("/subscriptions/{subscriptionId:guid}", async (
    Guid subscriptionId,
    [FromBody] UpdateSubscriptionRequest request,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var item = await dbContext.Subscriptions
        .FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

    if (item is null)
    {
        throw new KeyNotFoundException($"Subscription with ID {subscriptionId} not found.");
    }

    if (!string.IsNullOrWhiteSpace(request.PlanTier))
    {
        item.PlanTier = ParsePlanTier(request.PlanTier);
    }

    if (request.AmountUsd.HasValue)
    {
        item.AmountUsd = request.AmountUsd.Value;
    }

    if (request.ExpiresAtUtc.HasValue)
    {
        item.ExpiresAtUtc = request.ExpiresAtUtc;
    }

    if (request.IsActive.HasValue)
    {
        if (request.IsActive.Value)
        {
            var activeSubscriptions = await dbContext.Subscriptions
                .Where(x => x.UserId == item.UserId && x.IsActive && x.Id != item.Id)
                .ToListAsync(cancellationToken);

            foreach (var activeItem in activeSubscriptions)
            {
                activeItem.IsActive = false;
            }
        }

        item.IsActive = request.IsActive.Value;
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    var hasAnyActivePremium = await dbContext.Subscriptions
        .AsNoTracking()
        .AnyAsync(x => x.UserId == item.UserId && x.IsActive && x.PlanTier == PlanTier.PremiumSegmented, cancellationToken);

    if (hasAnyActivePremium)
    {
        await EnsurePremiumEntitlementsAsync(dbContext, item.UserId, cancellationToken);
    }
    else
    {
        await RevokePremiumEntitlementsAsync(dbContext, item.UserId, cancellationToken);
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    var refreshed = await dbContext.Subscriptions
        .AsNoTracking()
        .Include(x => x.User)
        .FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

    return refreshed is null ? Results.NotFound() : Results.Ok(MapSubscription(refreshed));
});

api.MapDelete("/subscriptions/{subscriptionId:guid}", async (
    Guid subscriptionId,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var item = await dbContext.Subscriptions
        .FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

    if (item is null)
    {
        return Results.NoContent();
    }

    var userId = item.UserId;
    dbContext.Subscriptions.Remove(item);
    await dbContext.SaveChangesAsync(cancellationToken);

    var hasAnyActivePremium = await dbContext.Subscriptions
        .AsNoTracking()
        .AnyAsync(x => x.UserId == userId && x.IsActive && x.PlanTier == PlanTier.PremiumSegmented, cancellationToken);

    if (hasAnyActivePremium)
    {
        await EnsurePremiumEntitlementsAsync(dbContext, userId, cancellationToken);
    }
    else
    {
        await RevokePremiumEntitlementsAsync(dbContext, userId, cancellationToken);
    }

    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
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

api.MapGet("/subscriptions", async (
    [FromQuery] bool? isActive,
    [FromQuery] int limit,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var safeLimit = limit <= 0 ? 50 : Math.Min(limit, 500);
    var query = dbContext.Subscriptions
        .AsNoTracking()
        .Include(x => x.User)
        .AsQueryable();

    if (isActive.HasValue)
    {
        query = query.Where(x => x.IsActive == isActive.Value);
    }

    var items = await query
        .OrderByDescending(x => x.ActivatedAtUtc)
        .Take(safeLimit)
        .ToListAsync(cancellationToken);

    return Results.Ok(items.Select(MapSubscription));
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
        request.Priority,
        request.ImageUrl,
        request.MapLink,
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
        request.Code,
        request.Name,
        request.Description,
        request.Latitude,
        request.Longitude,
        request.TriggerRadiusMeters,
        request.District,
        request.Priority,
        request.ImageUrl,
        request.MapLink,
        cancellationToken);

    return Results.Ok(MapPoiDetail(poi));
});

api.MapPost("/uploads/audio", async (
    HttpRequest request,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { message = "multipart/form-data is required." });
    }

    var form = await request.ReadFormAsync(cancellationToken);
    var file = form.Files["file"] ?? form.Files.FirstOrDefault();

    if (file is null || file.Length == 0)
    {
        return Results.BadRequest(new { message = "Audio file is required." });
    }

    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".m4a", ".ogg", ".aac"
    };

    if (!allowedExtensions.Contains(extension))
    {
        return Results.BadRequest(new { message = "Unsupported audio format. Allowed: .mp3, .wav, .m4a, .ogg, .aac" });
    }

    var audioDirectory = Path.Combine(environment.ContentRootPath, "Data", "uploads", "audio");
    Directory.CreateDirectory(audioDirectory);

    var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
    var targetPath = Path.Combine(audioDirectory, safeFileName);

    await using (var stream = File.Create(targetPath))
    {
        await file.CopyToAsync(stream, cancellationToken);
    }

    var relativePath = $"/media/audio/{safeFileName}";
    var absolutePath = $"{request.Scheme}://{request.Host}{relativePath}";

    return Results.Ok(new
    {
        fileName = safeFileName,
        filePath = absolutePath,
        relativePath,
        size = file.Length,
        contentType = file.ContentType
    });
});

api.MapDelete("/pois/{poiId:guid}", async (
    Guid poiId,
    IPoiService poiService,
    CancellationToken cancellationToken) =>
{
    await poiService.DeletePoiAsync(poiId, cancellationToken);
    return Results.NoContent();
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

api.MapPatch("/pois/{poiId:guid}/audios/{audioId:guid}", async (
    Guid poiId,
    Guid audioId,
    [FromBody] UpdateAudioRequest request,
    IPoiService poiService,
    CancellationToken cancellationToken) =>
{
    var updated = await poiService.UpdateAudioAsync(
        poiId,
        audioId,
        request.LanguageCode,
        request.FilePath,
        request.DurationSeconds,
        request.IsTextToSpeech,
        cancellationToken);

    return Results.Ok(MapAudioAsset(updated));
});

api.MapDelete("/pois/{poiId:guid}/audios/{audioId:guid}", async (
    Guid poiId,
    Guid audioId,
    IPoiService poiService,
    CancellationToken cancellationToken) =>
{
    await poiService.DeleteAudioAsync(poiId, audioId, cancellationToken);
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

api.MapPatch("/tours/{tourId:guid}", async (
    Guid tourId,
    [FromBody] UpdateTourRequest request,
    ITourService tourService,
    CancellationToken cancellationToken) =>
{
    var tour = await tourService.UpdateTourAsync(tourId, request.Name, request.Description, cancellationToken);
    return Results.Ok(MapTourSummary(tour));
});

api.MapDelete("/tours/{tourId:guid}", async (
    Guid tourId,
    ITourService tourService,
    CancellationToken cancellationToken) =>
{
    await tourService.DeleteTourAsync(tourId, cancellationToken);
    return Results.NoContent();
});

api.MapPatch("/tours/{tourId:guid}/stops/{stopId:guid}", async (
    Guid tourId,
    Guid stopId,
    [FromBody] UpdateTourStopRequest request,
    ITourService tourService,
    CancellationToken cancellationToken) =>
{
    var updated = await tourService.UpdateStopAsync(
        stopId,
        request.PoiId,
        request.Sequence,
        request.NextStopHint,
        cancellationToken);

    return Results.Ok(MapTourStop(updated));
});

api.MapDelete("/tours/{tourId:guid}/stops/{stopId:guid}", async (
    Guid tourId,
    Guid stopId,
    ITourService tourService,
    CancellationToken cancellationToken) =>
{
    await tourService.RemoveStopAsync(stopId, cancellationToken);
    return Results.NoContent();
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

api.MapGet("/sessions", async (
    [FromQuery] Guid? userId,
    [FromQuery] Guid? poiId,
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    [FromQuery] int? limit,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var safeLimit = !limit.HasValue || limit.Value <= 0 ? 200 : Math.Min(limit.Value, 1000);

    var query = dbContext.ListeningSessions
        .AsNoTracking()
        .Include(x => x.User)
        .Include(x => x.Poi)
        .AsQueryable();

    if (userId.HasValue)
    {
        query = query.Where(x => x.UserId == userId.Value);
    }

    if (poiId.HasValue)
    {
        query = query.Where(x => x.PoiId == poiId.Value);
    }

    if (startDate.HasValue)
    {
        query = query.Where(x => x.StartedAtUtc >= startDate.Value);
    }

    if (endDate.HasValue)
    {
        query = query.Where(x => x.StartedAtUtc <= endDate.Value);
    }

    var sessions = await query
        .OrderByDescending(x => x.StartedAtUtc)
        .Take(safeLimit)
        .Select(x => new
        {
            x.Id,
            x.UserId,
            userExternalRef = x.User != null ? x.User.ExternalRef : null,
            x.PoiId,
            poiName = x.Poi != null ? x.Poi.Name : null,
            x.StartedAtUtc,
            x.EndedAtUtc,
            x.DurationSeconds,
            triggerSource = x.TriggerSource.ToString()
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(sessions);
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

api.MapGet("/translations/{contentKey}/all", async (
    string contentKey,
    IContentTranslationService translationService,
    CancellationToken cancellationToken) =>
{
    var translations = await translationService.GetTranslationsByKeyAsync(contentKey, cancellationToken);
    return Results.Ok(translations);
});

api.MapGet("/translations", async (
    [FromQuery] string? contentKey,
    [FromQuery] string? languageCode,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var query = dbContext.ContentTranslations.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(contentKey))
    {
        var key = contentKey.Trim();
        query = query.Where(x => x.ContentKey.Contains(key));
    }

    if (!string.IsNullOrWhiteSpace(languageCode))
    {
        var lang = languageCode.Trim();
        query = query.Where(x => x.LanguageCode == lang);
    }

    var items = await query
        .OrderBy(x => x.ContentKey)
        .ThenBy(x => x.LanguageCode)
        .Take(500)
        .ToListAsync(cancellationToken);

    return Results.Ok(items);
});

api.MapDelete("/translations/{contentKey}/{languageCode}", async (
    string contentKey,
    string languageCode,
    IContentTranslationService translationService,
    CancellationToken cancellationToken) =>
{
    await translationService.DeleteTranslationAsync(contentKey, languageCode, cancellationToken);
    return Results.NoContent();
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
    poi.Priority,
    poi.District,
    poi.ImageUrl,
    poi.MapLink
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
    poi.Priority,
    poi.District,
    poi.ImageUrl,
    poi.MapLink,
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

object MapSubscription(Subscription subscription) => new
{
    subscription.Id,
    subscription.UserId,
    userExternalRef = subscription.User?.ExternalRef,
    planTier = subscription.PlanTier.ToString(),
    subscription.AmountUsd,
    subscription.IsActive,
    subscription.ActivatedAtUtc,
    subscription.ExpiresAtUtc
};

PlanTier ParsePlanTier(string planTierRaw)
{
    if (string.IsNullOrWhiteSpace(planTierRaw))
    {
        throw new ArgumentException("planTier is required.");
    }

    if (Enum.TryParse<PlanTier>(planTierRaw, true, out var parsed))
    {
        return parsed;
    }

    return planTierRaw.Trim() switch
    {
        "1" => PlanTier.Basic,
        "10" => PlanTier.PremiumSegmented,
        _ => throw new ArgumentException("Invalid planTier. Use Basic/PremiumSegmented or 1/10.")
    };
}

async Task EnsurePremiumEntitlementsAsync(
    AudioGuideDbContext dbContext,
    Guid userId,
    CancellationToken cancellationToken)
{
    var premiumSegments = await dbContext.FeatureSegments
        .Where(x => x.Code.StartsWith("premium."))
        .Select(x => new { x.Id })
        .ToListAsync(cancellationToken);

    if (premiumSegments.Count == 0)
    {
        return;
    }

    var segmentIds = premiumSegments.Select(x => x.Id).ToHashSet();

    var existing = await dbContext.UserEntitlements
        .Where(x => x.UserId == userId && segmentIds.Contains(x.FeatureSegmentId))
        .ToListAsync(cancellationToken);

    foreach (var segmentId in segmentIds)
    {
        var entitlement = existing.FirstOrDefault(x => x.FeatureSegmentId == segmentId);
        if (entitlement is null)
        {
            dbContext.UserEntitlements.Add(new UserEntitlement
            {
                UserId = userId,
                FeatureSegmentId = segmentId,
                GrantedAtUtc = DateTime.UtcNow
            });
            continue;
        }

        if (entitlement.RevokedAtUtc is not null)
        {
            entitlement.RevokedAtUtc = null;
            if (entitlement.GrantedAtUtc == default)
            {
                entitlement.GrantedAtUtc = DateTime.UtcNow;
            }
        }
    }
}

async Task RevokePremiumEntitlementsAsync(
    AudioGuideDbContext dbContext,
    Guid userId,
    CancellationToken cancellationToken)
{
    var premiumSegments = await dbContext.FeatureSegments
        .Where(x => x.Code.StartsWith("premium."))
        .Select(x => new { x.Id })
        .ToListAsync(cancellationToken);

    if (premiumSegments.Count == 0)
    {
        return;
    }

    var segmentIds = premiumSegments.Select(x => x.Id).ToHashSet();

    var entitlements = await dbContext.UserEntitlements
        .Where(x => x.UserId == userId && segmentIds.Contains(x.FeatureSegmentId) && x.RevokedAtUtc == null)
        .ToListAsync(cancellationToken);

    foreach (var entitlement in entitlements)
    {
        entitlement.RevokedAtUtc = DateTime.UtcNow;
    }
}

app.Run();

public sealed record CreatePoiRequest(
    string Code,
    string Name,
    double Latitude,
    double Longitude,
    double TriggerRadiusMeters = 30,
    string? Description = null,
    string? District = null,
    int Priority = 0,
    string? ImageUrl = null,
    string? MapLink = null);

public sealed record UpdatePoiRequest(
    string? Code,
    string? Name,
    string? Description,
    double? Latitude,
    double? Longitude,
    double? TriggerRadiusMeters,
    string? District,
    int? Priority,
    string? ImageUrl,
    string? MapLink);

public sealed record AssignAudioRequest(
    string LanguageCode,
    string FilePath,
    int DurationSeconds,
    bool IsTextToSpeech = false);

public sealed record UpdateAudioRequest(
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

public sealed record UpdateTourRequest(
    string? Name,
    string? Description);

public sealed record UpdateTourStopRequest(
    Guid PoiId,
    int Sequence,
    string? NextStopHint = null);

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

public sealed record CreateSubscriptionRequest(
    Guid UserId,
    string PlanTier,
    decimal AmountUsd,
    bool IsActive = true,
    DateTime? ExpiresAtUtc = null);

public sealed record UpdateSubscriptionRequest(
    string? PlanTier,
    decimal? AmountUsd,
    bool? IsActive,
    DateTime? ExpiresAtUtc);

public sealed class DomainExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<DomainExceptionFilter>>();

        try
        {
            return await next(context);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Bad request at {Method} {Path}", context.HttpContext.Request.Method, context.HttpContext.Request.Path);
            return Results.BadRequest(new ProblemDetails
            {
                Title = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogInformation(ex, "Not found at {Method} {Path}", context.HttpContext.Request.Method, context.HttpContext.Request.Path);
            return Results.NotFound(new ProblemDetails
            {
                Title = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Forbidden access at {Method} {Path}", context.HttpContext.Request.Method, context.HttpContext.Request.Path);
            return Results.Problem(
                title: ex.Message,
                statusCode: StatusCodes.Status403Forbidden);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogWarning(ex, "Audio/content file missing at {Method} {Path}", context.HttpContext.Request.Method, context.HttpContext.Request.Path);
            return Results.Problem(
                title: ex.Message,
                detail: ex.FileName,
                statusCode: StatusCodes.Status409Conflict);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Conflict at {Method} {Path}", context.HttpContext.Request.Method, context.HttpContext.Request.Path);
            return Results.Problem(
                title: ex.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }
}
