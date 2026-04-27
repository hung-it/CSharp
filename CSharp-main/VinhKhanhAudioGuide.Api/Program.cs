using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using VinhKhanhAudioGuide.Backend.Application.Services;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Domain.Enums;
using VinhKhanhAudioGuide.Backend.Infrastructure;
using VinhKhanhAudioGuide.Backend.Persistence;
using PoiGeofenceEventEntity = VinhKhanhAudioGuide.Backend.Domain.Entities.PoiGeofenceEvent;

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

            // Support wildcard patterns like *.trycloudflare.com
            foreach (var allowed in normalizedAllowedOrigins)
            {
                if (allowed.StartsWith("*."))
                {
                    var suffix = allowed[1..]; // remove leading *
                    if (origin.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // In local development, allow localhost/127.0.0.1 on any port (e.g. Vite 5173/5174).
            if (builder.Environment.IsDevelopment() && Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            {
                return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("10.0.2.2", StringComparison.OrdinalIgnoreCase);
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

api.MapPost("/admin/cleanup-sessions", async (
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var orphaned = await dbContext.ListeningSessions
        .Where(s => s.EndedAtUtc == null)
        .ToListAsync(cancellationToken);

    foreach (var session in orphaned)
    {
        session.EndedAtUtc = session.StartedAtUtc.AddSeconds(30);
        session.DurationSeconds = 30;
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new { cleaned = orphaned.Count });
});

api.MapPost("/admin/cleanup-anon-users", async (
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var anonUsers = await dbContext.Users
        .Where(u => string.IsNullOrWhiteSpace(u.Username))
        .ToListAsync(cancellationToken);

    var anonUserIds = anonUsers.Select(u => u.Id).ToList();

    if (anonUserIds.Count == 0)
    {
        return Results.Ok(new { deletedUsers = 0, deletedSessions = 0, deletedSubscriptions = 0 });
    }

    var sessionsDeleted = await dbContext.ListeningSessions
        .Where(s => anonUserIds.Contains(s.UserId))
        .ExecuteDeleteAsync(cancellationToken);

    var subsDeleted = await dbContext.Subscriptions
        .Where(s => anonUserIds.Contains(s.UserId))
        .ExecuteDeleteAsync(cancellationToken);

    var usersDeleted = await dbContext.Users
        .Where(u => anonUserIds.Contains(u.Id))
        .ExecuteDeleteAsync(cancellationToken);

    return Results.Ok(new
    {
        deletedUsers = anonUsers.Count,
        deletedSessions = sessionsDeleted,
        deletedSubscriptions = subsDeleted
    });
});

api.MapPost("/users/resolve", async (
    HttpContext httpContext,
    [FromBody] ResolveUserRequest request,
    AudioGuideDbContext dbContext,
    ISubscriptionService subscriptionService,
    CancellationToken cancellationToken) =>
{
    // Accept both Username and ExternalRef for backward compatibility
    var identifier = request.Username ?? request.ExternalRef;
    
    if (string.IsNullOrWhiteSpace(identifier))
    {
        return Results.BadRequest(new { success = false, message = "username is required." });
    }

    var user = await dbContext.Users.FirstOrDefaultAsync(x => 
        x.Username == identifier || x.ExternalRef == identifier, cancellationToken);

    if (user is null)
    {
        // Create new user with password if provided
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user = new User
            {
                Username = identifier,
                ExternalRef = $"USER_{Guid.NewGuid():N}",
                PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage) ? "vi" : request.PreferredLanguage,
                PasswordHash = HashPassword(request.Password)
            };
        }
        else
        {
            return Results.Ok(new
            {
                success = false,
                message = "Tài khoản không tồn tại."
            });
        }

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    else
    {
        // Verify password
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                // First login with password - set the password
                user.PasswordHash = HashPassword(request.Password);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                return Results.Ok(new
                {
                    success = false,
                    message = "Mật khẩu không đúng."
                });
            }
        }
        else if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            // Only require password when the caller is NOT already authenticated.
            // If the request includes an X-User-Id header (admin/session is valid),
            // skip the password check and allow lookup.
            var hasValidSession = httpContext.Request.Headers.TryGetValue("X-User-Id", out var callerId)
                && Guid.TryParse(callerId.ToString(), out _);

            if (!hasValidSession)
            {
                return Results.Ok(new
                {
                    success = false,
                    message = "Vui lòng nhập mật khẩu."
                });
            }
        }
    }

    // Get user's subscription/plan info
    var planTier = "Basic";
    try
    {
        var subscription = await subscriptionService.GetActiveSubscriptionAsync(user.Id, cancellationToken);
        if (subscription != null)
        {
            planTier = subscription.PlanTier.ToString();
        }
    }
    catch
    {
        // No subscription found, use default Basic
    }

    return Results.Ok(new
    {
        success = true,
        id = user.Id,
        user.Username,
        role = user.Role.ToString(),
        user.PreferredLanguage,
        user.CreatedAtUtc,
        plan = planTier
    });
});

api.MapPost("/users/anonymous", async (
    AudioGuideDbContext dbContext,
    ISubscriptionService subscriptionService,
    CancellationToken cancellationToken) =>
{
    var tempUsername = $"Guest_{Guid.NewGuid():N}";
    var tempPassword = Guid.NewGuid().ToString();

    var user = new User
    {
        Username = tempUsername,
        ExternalRef = $"ANON_{Guid.NewGuid():N}",
        PasswordHash = HashPassword(tempPassword),
        Role = UserRole.EndUser,
        PreferredLanguage = "vi",
        CreatedAtUtc = DateTime.UtcNow
    };

    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync(cancellationToken);

    // Activate Basic subscription for anonymous user
    var planTier = "Basic";
    try
    {
        var subscription = await subscriptionService.ActivateSubscriptionAsync(user.Id, PlanTier.Basic, 0, cancellationToken);
        planTier = subscription.PlanTier.ToString();
    }
    catch { }

    return Results.Ok(new
    {
        success = true,
        id = user.Id,
        username = user.Username,
        password = tempPassword,
        role = user.Role.ToString(),
        preferredLanguage = user.PreferredLanguage,
        createdAtUtc = user.CreatedAtUtc,
        plan = planTier
    });
});

api.MapPost("/users/register", async (
    [FromBody] RegisterRequest request,
    AudioGuideDbContext dbContext,
    ISubscriptionService subscriptionService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { success = false, message = "Username và mật khẩu không được để trống." });
    }

    var username = request.Username.Trim();

    if (username.Length < 3)
    {
        return Results.BadRequest(new { success = false, message = "Tên đăng nhập phải có ít nhất 3 ký tự." });
    }

    if (request.Password.Length < 4)
    {
        return Results.BadRequest(new { success = false, message = "Mật khẩu phải có ít nhất 4 ký tự." });
    }

    // Check duplicate username
    var existing = await dbContext.Users.FirstOrDefaultAsync(
        x => x.Username == username, cancellationToken);

    if (existing != null)
    {
        return Results.BadRequest(new { success = false, message = "Tên đăng nhập đã được sử dụng." });
    }

    var user = new User
    {
        Username = username,
        ExternalRef = $"REG_{Guid.NewGuid():N}",
        PasswordHash = HashPassword(request.Password),
        PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage) ? "vi" : request.PreferredLanguage,
        Role = UserRole.EndUser,
        CreatedAtUtc = DateTime.UtcNow
    };

    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync(cancellationToken);

    // Auto-create Basic subscription for new user
    try
    {
        await subscriptionService.ActivateSubscriptionAsync(user.Id, PlanTier.Basic, 1, cancellationToken);
    }
    catch
    {
        // Log but don't fail registration if subscription creation fails
    }

    return Results.Created($"/api/v1/users/{user.Id}", new
    {
        success = true,
        id = user.Id,
        user.Username,
        role = user.Role.ToString(),
        preferredLanguage = user.PreferredLanguage,
        createdAtUtc = user.CreatedAtUtc,
        plan = "Basic",
        message = "Đăng ký thành công!"
    });
});

api.MapPost("/users/change-password", async (
    [FromBody] ChangePasswordRequest request,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) ||
        string.IsNullOrWhiteSpace(request.CurrentPassword) ||
        string.IsNullOrWhiteSpace(request.NewPassword))
    {
        return Results.BadRequest(new { success = false, message = "All fields are required." });
    }

    var username = request.Username.Trim();
    var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

    if (user is null)
    {
        return Results.Ok(new { success = false, message = "Tài khoản không tồn tại." });
    }

    // Verify current password if user has one set
    if (!string.IsNullOrWhiteSpace(user.PasswordHash))
    {
        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return Results.Ok(new { success = false, message = "Mật khẩu hiện tại không đúng." });
        }
    }

    // Set new password
    user.PasswordHash = HashPassword(request.NewPassword);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new { success = true, message = "Đổi mật khẩu thành công." });
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
        role = user.Role.ToString(),
        user.PreferredLanguage,
        user.CreatedAtUtc
    });
});

api.MapGet("/users/me/pois", async (HttpContext httpContext, IPoiService poiService, CancellationToken cancellationToken) =>
{
    if (!httpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
        !Guid.TryParse(userIdHeader.ToString(), out var userId))
    {
        return Results.BadRequest(new { message = "X-User-Id header with valid GUID is required." });
    }

    var pois = await poiService.GetPoisByManagerAsync(userId, cancellationToken);

    return Results.Ok(new
    {
        hasPois = pois.Any(),
        pois = pois.Select(p => new
        {
            p.Id,
            p.Code,
            p.Name,
            p.Description,
            p.District,
            p.Latitude,
            p.Longitude,
            p.Priority,
            p.TriggerRadiusMeters,
            p.ImageUrl,
            p.MapLink
        }),
        totalCount = pois.Count()
    });
});

api.MapGet("/shops", async (AudioGuideDbContext dbContext, CancellationToken cancellationToken) =>
{
    var shops = await dbContext.ShopProfiles
        .AsNoTracking()
        .Include(s => s.ManagerUser)
        .OrderBy(s => s.Name)
        .Select(s => new
        {
            s.Id,
            s.Name,
            s.Address,
            s.Description,
            s.VerificationStatus,
            s.ManagerUserId,
            managerUsername = s.ManagerUser != null ? s.ManagerUser.Username : null,
            poiCount = dbContext.Pois.Count(p => p.ShopId == s.Id)
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(shops);
});

api.MapPost("/shops", async (
    HttpContext httpContext,
    [FromBody] CreateShopRequest request,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    // Only Admin can create shops
    if (!httpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
        !Guid.TryParse(userIdHeader.ToString(), out var adminUserId))
    {
        return Results.BadRequest(new { message = "X-User-Id header with valid GUID is required." });
    }

    var admin = await dbContext.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == adminUserId, cancellationToken);

    if (admin is null || admin.Role != UserRole.Admin)
    {
        return Results.Forbid();
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "Shop name is required." });
    }

    // Check if manager user exists and is ShopManager
    User? managerUser = null;
    if (request.ManagerUserId.HasValue)
    {
        managerUser = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == request.ManagerUserId.Value, cancellationToken);

        if (managerUser is null)
        {
            return Results.BadRequest(new { message = "Manager user not found." });
        }

        if (managerUser.Role != UserRole.ShopManager)
        {
            return Results.BadRequest(new { message = "User must be a ShopManager to be assigned as shop manager." });
        }

        // Check if this manager already has a shop
        var existingShop = await dbContext.ShopProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ManagerUserId == managerUser.Id, cancellationToken);

        if (existingShop is not null)
        {
            return Results.Conflict(new { message = "This manager already has a shop assigned." });
        }
    }

    var shop = new ShopProfile
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        ExternalRef = $"SHOP_{Guid.NewGuid():N}",
        Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
        Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        MapLink = string.IsNullOrWhiteSpace(request.MapLink) ? null : request.MapLink.Trim(),
        Latitude = request.Latitude,
        Longitude = request.Longitude,
        ManagerUserId = request.ManagerUserId,
        VerificationStatus = ShopVerificationStatus.Pending,
        CreatedAtUtc = DateTime.UtcNow
    };

    dbContext.ShopProfiles.Add(shop);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/v1/shops/{shop.Id}", new
    {
        shop.Id,
        shop.Name,
        shop.Address,
        shop.Description,
        shop.MapLink,
        shop.Latitude,
        shop.Longitude,
        shop.ManagerUserId,
        managerUsername = managerUser?.Username,
        shop.VerificationStatus,
        message = "Shop created successfully."
    });
});

api.MapGet("/users/shop-managers", async (AudioGuideDbContext dbContext, CancellationToken cancellationToken) =>
{
    var shopManagers = await dbContext.Users
        .AsNoTracking()
        .Where(u => u.Role == UserRole.ShopManager)
        .Select(u => new
        {
            u.Id,
            u.Username,
            u.PreferredLanguage,
            u.CreatedAtUtc,
            hasShop = dbContext.Pois.Any(p => p.ManagerUserId == u.Id)
        })
        .OrderBy(u => u.Username)
        .ToListAsync(cancellationToken);

    return Results.Ok(shopManagers);
});

api.MapPost("/users/create-shop-manager", async (
    HttpContext httpContext,
    [FromBody] CreateShopManagerRequest request,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    // Only Admin can create shop managers
    if (!httpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
        !Guid.TryParse(userIdHeader.ToString(), out var adminUserId))
    {
        return Results.BadRequest(new { message = "X-User-Id header with valid GUID is required." });
    }

    var admin = await dbContext.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == adminUserId, cancellationToken);

    if (admin is null || admin.Role != UserRole.Admin)
    {
        return Results.Forbid();
    }

    if (string.IsNullOrWhiteSpace(request.Username))
    {
        return Results.BadRequest(new { message = "Username is required." });
    }

    var username = request.Username.Trim();

    // Check if username already exists
    var existingUser = await dbContext.Users
        .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

    if (existingUser is not null)
    {
        return Results.Conflict(new { message = "Username already exists." });
    }

    var user = new User
    {
        Id = Guid.NewGuid(),
        Username = username,
        ExternalRef = $"SHOP_MANAGER_{Guid.NewGuid():N}",
        PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage) ? "vi" : request.PreferredLanguage,
        PasswordHash = !string.IsNullOrWhiteSpace(request.Password) ? HashPassword(request.Password) : null,
        Role = UserRole.ShopManager,
        CreatedAtUtc = DateTime.UtcNow
    };

    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/v1/users/{user.Id}", new
    {
        user.Id,
        user.Username,
        role = user.Role.ToString(),
        user.PreferredLanguage,
        user.CreatedAtUtc,
        message = "Shop Manager created successfully."
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
        query = query.Where(x => 
            (x.Username != null && x.Username.Contains(keyword)) || 
            (x.ExternalRef != null && x.ExternalRef.Contains(keyword)));
    }

    var users = await query
        .OrderByDescending(x => x.CreatedAtUtc)
        .Take(safeLimit)
        .Select(x => new
        {
            x.Id,
            x.Username,
            x.ExternalRef,
            role = x.Role.ToString(),
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

api.MapPost("/admin/fix-external-refs", async (
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    // Fix ExternalRef for admin and owner accounts
    var admin = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == "admin", cancellationToken);
    if (admin != null && !admin.ExternalRef.StartsWith("ADMIN_USER"))
    {
        admin.ExternalRef = "ADMIN_USER";
    }

    var owner1 = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == "owner1", cancellationToken);
    if (owner1 != null && !owner1.ExternalRef.StartsWith("SHOP_MANAGER_OWNER"))
    {
        owner1.ExternalRef = "SHOP_MANAGER_OWNER1";
    }

    var owner2 = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == "owner2", cancellationToken);
    if (owner2 != null && !owner2.ExternalRef.StartsWith("SHOP_MANAGER_OWNER"))
    {
        owner2.ExternalRef = "SHOP_MANAGER_OWNER2";
    }

    var owner3 = await dbContext.Users.FirstOrDefaultAsync(x => x.Username == "owner3", cancellationToken);
    if (owner3 != null && !owner3.ExternalRef.StartsWith("SHOP_MANAGER_OWNER"))
    {
        owner3.ExternalRef = "SHOP_MANAGER_OWNER3";
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new { message = "External refs updated for admin/owners." });
});

api.MapPost("/admin/reset-and-seed", async (
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    // Clear existing data
    dbContext.ListeningSessions.RemoveRange(dbContext.ListeningSessions);
    dbContext.RoutePoints.RemoveRange(dbContext.RoutePoints);
    dbContext.UserEntitlements.RemoveRange(dbContext.UserEntitlements);
    dbContext.Subscriptions.RemoveRange(dbContext.Subscriptions);
    dbContext.TourStops.RemoveRange(dbContext.TourStops);
    dbContext.Tours.RemoveRange(dbContext.Tours);
    dbContext.AudioAssets.RemoveRange(dbContext.AudioAssets);
    dbContext.Pois.RemoveRange(dbContext.Pois);
    dbContext.ShopProfiles.RemoveRange(dbContext.ShopProfiles);
    dbContext.Users.RemoveRange(dbContext.Users);
    dbContext.FeatureSegments.RemoveRange(dbContext.FeatureSegments);
    await dbContext.SaveChangesAsync(cancellationToken);

    // Re-seed
    var dataSeeder = new DataSeeder(
        dbContext,
        new PoiService(dbContext),
        new TourService(dbContext),
        new SubscriptionService(dbContext));
    await dataSeeder.SeedAsync(cancellationToken);

    var users = await dbContext.Users
        .AsNoTracking()
        .Where(x => x.ExternalRef == "ADMIN_USER" || x.ExternalRef.StartsWith("SHOP_MANAGER_OWNER"))
        .OrderBy(x => x.ExternalRef)
        .Select(x => new
        {
            x.Id,
            x.ExternalRef,
            role = x.Role.ToString(),
            x.PreferredLanguage
        })
        .ToListAsync(cancellationToken);

    var shops = await dbContext.ShopProfiles
        .AsNoTracking()
        .Select(x => new { x.Id, x.ExternalRef, x.Name })
        .ToListAsync(cancellationToken);

    var pois = await dbContext.Pois
        .AsNoTracking()
        .Select(x => new { x.Id, x.Code, x.Name, x.District, x.ShopId })
        .ToListAsync(cancellationToken);

    return Results.Ok(new
    {
        message = "Reset và seed thành công!",
        users,
        shops,
        poiCount = pois.Count(),
        poisByDistrict = pois.GroupBy(p => p.District)
            .ToDictionary(g => g.Key ?? "Unknown", g => g.ToList())
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
    [FromQuery] Guid? managerId,
    IPoiService poiService,
    CancellationToken cancellationToken) =>
{
    IEnumerable<Poi> pois;

    if (managerId.HasValue && managerId.Value != Guid.Empty)
    {
        // Get POIs by manager (Shop Owner)
        pois = await poiService.GetPoisByManagerAsync(managerId.Value, cancellationToken);
        // Apply district filter if provided
        if (!string.IsNullOrWhiteSpace(district))
        {
            pois = pois.Where(p => p.District == district);
        }
    }
    else
    {
        // Get all POIs (Admin view)
        pois = string.IsNullOrWhiteSpace(district)
            ? await poiService.GetAllPoiAsync(cancellationToken)
            : await poiService.GetPoisByDistrictAsync(district, cancellationToken);
    }

    return Results.Ok(pois.Select(MapPoiSummary));
});

api.MapGet("/pois/{poiId:guid}", async (Guid poiId, IPoiService poiService, CancellationToken cancellationToken) =>
{
    var poi = await poiService.GetPoiByIdAsync(poiId, cancellationToken);
    return poi is null ? Results.NotFound() : Results.Ok(MapPoiDetail(poi));
});

api.MapPost("/pois", async (
    HttpContext httpContext,
    [FromBody] CreatePoiRequest request,
    IPoiService poiService,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (!httpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
        !Guid.TryParse(userIdHeader.ToString(), out var userId))
    {
        return Results.BadRequest(new { message = "X-User-Id header with valid GUID is required." });
    }

    var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    if (user is null)
    {
        return Results.NotFound(new { message = "User not found." });
    }

    Guid? managerUserId = null;

    if (user.Role == UserRole.Admin)
    {
        // Admin can create POI for a specific owner
        if (request.ManagerUserId.HasValue && request.ManagerUserId != Guid.Empty)
        {
            var targetOwner = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.ManagerUserId.Value && u.Role == UserRole.ShopManager, cancellationToken);

            if (targetOwner is null)
            {
                return Results.BadRequest(new { message = "Shop Manager not found." });
            }
            managerUserId = request.ManagerUserId;
        }
        // Admin can also create without manager
    }
    else if (user.Role == UserRole.ShopManager)
    {
        // Shop Manager creates POI for themselves
        managerUserId = userId;
    }

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
        managerUserId,
        cancellationToken);

    return Results.Created($"/api/v1/pois/{poi.Id}", MapPoiDetail(poi));
});

api.MapPatch("/pois/{poiId:guid}", async (
    Guid poiId,
    HttpContext httpContext,
    [FromBody] UpdatePoiRequest request,
    IPoiService poiService,
    IPoiAuthorizationService authService,
    CancellationToken cancellationToken) =>
{
    // Get userId from header
    if (!httpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
        !Guid.TryParse(userIdHeader.ToString(), out var userId))
    {
        return Results.BadRequest(new { message = "X-User-Id header with valid GUID is required." });
    }

    // Check authorization
    var canManage = await authService.CanManagePoiAsync(userId, cancellationToken);
    if (!canManage)
    {
        return Results.Forbid();
    }

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
    HttpContext httpContext,
    IPoiService poiService,
    IPoiAuthorizationService authService,
    CancellationToken cancellationToken) =>
{
    // Get userId from header
    if (!httpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
        !Guid.TryParse(userIdHeader.ToString(), out var userId))
    {
        return Results.BadRequest(new { message = "X-User-Id header with valid GUID is required." });
    }

    // Check authorization
    var canManage = await authService.CanManagePoiAsync(userId, cancellationToken);
    if (!canManage)
    {
        return Results.Forbid();
    }

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
    HttpContext httpContext,
    [FromBody] AssignAudioRequest request,
    IPoiService poiService,
    IPoiAuthorizationService authService,
    CancellationToken cancellationToken) =>
{
    // Get userId from header
    if (!httpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
        !Guid.TryParse(userIdHeader.ToString(), out var userId))
    {
        return Results.BadRequest(new { message = "X-User-Id header with valid GUID is required." });
    }

    // Check authorization
    var canManage = await authService.CanManagePoiAsync(userId, cancellationToken);
    if (!canManage)
    {
        return Results.Forbid();
    }

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
    HttpContext httpContext,
    [FromBody] UpdateAudioRequest request,
    IPoiService poiService,
    IPoiAuthorizationService authService,
    CancellationToken cancellationToken) =>
{
    // Get userId from header
    if (!httpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
        !Guid.TryParse(userIdHeader.ToString(), out var userId))
    {
        return Results.BadRequest(new { message = "X-User-Id header with valid GUID is required." });
    }

    // Check authorization
    var canManage = await authService.CanManagePoiAsync(userId, cancellationToken);
    if (!canManage)
    {
        return Results.Forbid();
    }

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
    HttpContext httpContext,
    IPoiService poiService,
    IPoiAuthorizationService authService,
    CancellationToken cancellationToken) =>
{
    // Get userId from header
    if (!httpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
        !Guid.TryParse(userIdHeader.ToString(), out var userId))
    {
        return Results.BadRequest(new { message = "X-User-Id header with valid GUID is required." });
    }

    // Check authorization
    var canManage = await authService.CanManagePoiAsync(userId, cancellationToken);
    if (!canManage)
    {
        return Results.Forbid();
    }

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
    var tour = await tourService.CreateTourAsync(request.Code, request.Name, request.Description, request.ShopId, cancellationToken);
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
            userName = x.User != null ? x.User.Username : null,
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

api.MapGet("/sessions/active", async (
    IListeningSessionService sessionService,
    CancellationToken cancellationToken) =>
{
    var countsByPoi = await sessionService.GetActiveSessionCountsByPoiAsync(cancellationToken);
    var total = await sessionService.GetTotalActiveSessionCountAsync(cancellationToken);
    var byPoi = countsByPoi.Select(kv => new { poiId = kv.Key, activeCount = kv.Value }).ToList();
    return Results.Ok(new { totalActive = total, byPoi });
});

api.MapPost("/qr/start", async (
    [FromBody] StartQrRequest request,
    IQrPlaybackService qrService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await qrService.StartSessionByQrAsync(
            request.UserId,
            request.QrPayload,
            request.LanguageCode,
            cancellationToken);

        if (result.Content == null)
        {
            return Results.BadRequest(new { error = "Không tìm thấy nội dung cho QR này" });
        }

        return Results.Ok(new
        {
            session = result.Session != null ? MapListeningSession(result.Session) : null,
            content = new
            {
                result.Content.PoiId,
                result.Content.PoiCode,
                result.Content.PoiName,
                result.Content.AudioPath,
                result.Content.IsTextToSpeech
            }
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

api.MapGet("/analytics/top", async (
    [FromQuery] int limit,
    [FromQuery] Guid? managerId,
    IAnalyticsService analytics,
    CancellationToken cancellationToken) =>
{
    var result = await analytics.GetTopPoisByListeningCountAsync(limit <= 0 ? 5 : limit, managerId, cancellationToken);
    return Results.Ok(result);
});

api.MapGet("/analytics/pois", async (
    [FromQuery] Guid? managerId,
    IAnalyticsService analytics,
    CancellationToken cancellationToken) =>
{
    var result = await analytics.GetPoiListeningStatsAsync(managerId, cancellationToken);
    return Results.Ok(result);
});

api.MapGet("/analytics/visits/top", async (
    [FromQuery] int limit,
    [FromQuery] Guid? managerId,
    IAnalyticsService analytics,
    CancellationToken cancellationToken) =>
{
    var result = await analytics.GetTopPoisByVisitCountAsync(limit <= 0 ? 5 : limit, managerId, cancellationToken);
    return Results.Ok(result);
});

api.MapGet("/analytics/visits/pois", async (
    [FromQuery] Guid? managerId,
    IAnalyticsService analytics,
    CancellationToken cancellationToken) =>
{
    var result = await analytics.GetPoiVisitStatsAsync(managerId, cancellationToken);
    return Results.Ok(result);
});

api.MapGet("/analytics/visits/pois/{poiId:guid}", async (
    Guid poiId,
    IAnalyticsService analytics,
    CancellationToken cancellationToken) =>
{
    var result = await analytics.GetPoiVisitStatAsync(poiId, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

api.MapGet("/analytics/daily", async (
    [FromQuery] int days,
    [FromQuery] Guid? managerId,
    IAnalyticsService analytics,
    CancellationToken cancellationToken) =>
{
    var result = await analytics.GetDailyStatsAsync(days <= 0 ? 7 : days, managerId, cancellationToken);
    return Results.Ok(result);
});

api.MapGet("/analytics/tours", async (
    [FromQuery] Guid? managerId,
    IAnalyticsService analytics,
    CancellationToken cancellationToken) =>
{
    var result = await analytics.GetTourViewStatsAsync(managerId, cancellationToken);
    return Results.Ok(result);
});

api.MapGet("/analytics/geofence", async (
    [FromQuery] Guid? poiId,
    [FromQuery] Guid? managerId,
    IAnalyticsService analytics,
    CancellationToken cancellationToken) =>
{
    var result = await analytics.GetGeofenceStatsAsync(poiId, managerId, cancellationToken);
    return Results.Ok(result);
});

api.MapGet("/analytics/summary", async (
    [FromQuery] int days,
    [FromQuery] Guid? managerId,
    IAnalyticsService analytics,
    CancellationToken cancellationToken) =>
{
    var result = await analytics.GetUsageSummaryAsync(days <= 0 ? 7 : days, managerId, cancellationToken);
    return Results.Ok(result);
});

api.MapGet("/analytics/usage", async (
    [FromQuery] int days,
    [FromQuery] Guid? managerId,
    AudioGuideDbContext dbContext,
    IAnalyticsService analytics,
    CancellationToken cancellationToken) =>
{
    var totalDays = days <= 0 ? 7 : days;
    var now = DateTime.UtcNow;
    var start = now.AddDays(-totalDays);

    // Get heatmap data
    var heatmap = await analytics.GetHeatmapDataAsync(start, now, 3, managerId, cancellationToken);

    // Filter by manager's POIs if specified
    int totalListens;
    int activeCells;
    
    if (managerId.HasValue && managerId.Value != Guid.Empty)
    {
        var managerPoiIds = await dbContext.Pois
            .AsNoTracking()
            .Where(p => p.ManagerUserId == managerId.Value)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var managerSessions = await dbContext.ListeningSessions
            .AsNoTracking()
            .Where(s => s.StartedAtUtc >= start && managerPoiIds.Contains(s.PoiId))
            .CountAsync(cancellationToken);

        totalListens = managerSessions;
        activeCells = managerPoiIds.Count;
    }
    else
    {
        totalListens = heatmap.Sum(x => x.PointCount);
        activeCells = heatmap.Count();
    }

    return Results.Ok(new
    {
        days = totalDays,
        totalListens,
        activeCells,
        startDateUtc = start,
        endDateUtc = now
    });
});

api.MapGet("/analytics/heatmap", async (
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    [FromQuery] int precision,
    [FromQuery] Guid? managerId,
    IAnalyticsService analytics,
    CancellationToken cancellationToken) =>
{
    var result = await analytics.GetHeatmapDataAsync(startDate, endDate, precision <= 0 ? 3 : precision, managerId, cancellationToken);
    return Results.Ok(result);
});

// Visit Tracking Endpoints
api.MapPost("/visits/start", async (
    [FromBody] StartVisitRequest request,
    IVisitTrackingService visitService,
    CancellationToken cancellationToken) =>
{
    var visit = await visitService.StartVisitAsync(
        request.UserId,
        request.PoiId,
        Enum.TryParse<VisitTriggerSource>(request.TriggerSource, true, out var ts) ? ts : VisitTriggerSource.Map,
        Enum.TryParse<PageSource>(request.PageSource, true, out var ps) ? ps : PageSource.Map,
        request.Latitude,
        request.Longitude,
        request.AnonymousRef,
        cancellationToken);

    return Results.Created($"/api/v1/visits/{visit.Id}", MapVisitSession(visit));
});

api.MapPost("/visits/{visitId:guid}/end", async (
    Guid visitId,
    IVisitTrackingService visitService,
    CancellationToken cancellationToken) =>
{
    var visit = await visitService.EndVisitAsync(visitId, cancellationToken);
    return Results.Ok(MapVisitSession(visit));
});

api.MapPost("/visits/{visitId:guid}/audio", async (
    Guid visitId,
    [FromBody] UpdateVisitAudioRequest request,
    IVisitTrackingService visitService,
    CancellationToken cancellationToken) =>
{
    var visit = await visitService.UpdateVisitWithAudioDataAsync(
        visitId,
        request.ListeningSessionCount,
        request.TotalListenDurationSeconds,
        cancellationToken);
    return Results.Ok(MapVisitSession(visit));
});

api.MapGet("/visits", async (
    [FromQuery] Guid? userId,
    [FromQuery] Guid? poiId,
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var query = dbContext.VisitSessions
        .AsNoTracking()
        .Include(v => v.User)
        .Include(v => v.Poi)
        .AsQueryable();

    if (userId.HasValue)
    {
        query = query.Where(v => v.UserId == userId.Value);
    }

    if (poiId.HasValue)
    {
        query = query.Where(v => v.PoiId == poiId.Value);
    }

    if (startDate.HasValue)
    {
        query = query.Where(v => v.VisitedAtUtc >= startDate.Value);
    }

    if (endDate.HasValue)
    {
        query = query.Where(v => v.VisitedAtUtc <= endDate.Value);
    }

    var visits = await query
        .OrderByDescending(v => v.VisitedAtUtc)
        .Take(200)
        .ToListAsync(cancellationToken);

    return Results.Ok(visits.Select(MapVisitSession));
});

// Tour View Tracking Endpoints
api.MapPost("/tours/{tourId:guid}/view/start", async (
    Guid tourId,
    [FromBody] StartTourViewRequest request,
    IVisitTrackingService visitService,
    CancellationToken cancellationToken) =>
{
    var tourView = await visitService.StartTourViewAsync(
        request.UserId,
        tourId,
        request.AnonymousRef,
        cancellationToken);

    return Results.Created($"/api/v1/tours/{tourId}/view/{tourView.Id}", MapTourViewSession(tourView));
});

api.MapPost("/tours/{tourId:guid}/view/{viewId:guid}/end", async (
    Guid tourId,
    Guid viewId,
    [FromBody] EndTourViewRequest request,
    IVisitTrackingService visitService,
    CancellationToken cancellationToken) =>
{
    var tourView = await visitService.EndTourViewAsync(
        viewId,
        request.PoiVisitedCount,
        request.AudioListenedCount,
        cancellationToken);
    return Results.Ok(MapTourViewSession(tourView));
});

// Geofence Event Tracking
api.MapPost("/geofence/events", async (
    [FromBody] RecordGeofenceEventRequest request,
    IVisitTrackingService visitService,
    CancellationToken cancellationToken) =>
{
    var geofenceEvent = await visitService.RecordGeofenceEventAsync(
        request.UserId,
        request.PoiId,
        Enum.TryParse<PoiGeofenceEventType>(request.EventType, true, out var et) ? et : PoiGeofenceEventType.Enter,
        request.Latitude,
        request.Longitude,
        request.DistanceFromCenterMeters,
        request.AnonymousRef,
        cancellationToken);

    return Results.Created($"/api/v1/geofence/events/{geofenceEvent.Id}", MapGeofenceEvent(geofenceEvent));
});

api.MapGet("/geofence/events", async (
    [FromQuery] Guid poiId,
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    AudioGuideDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var query = dbContext.PoiGeofenceEvents
        .AsNoTracking()
        .Include(g => g.User)
        .Include(g => g.Poi)
        .Where(g => g.PoiId == poiId);

    if (startDate.HasValue)
    {
        query = query.Where(g => g.OccurredAtUtc >= startDate.Value);
    }

    if (endDate.HasValue)
    {
        query = query.Where(g => g.OccurredAtUtc <= endDate.Value);
    }

    var events = await query
        .OrderByDescending(g => g.OccurredAtUtc)
        .Take(200)
        .ToListAsync(cancellationToken);

    return Results.Ok(events.Select(MapGeofenceEvent));
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

    var translations = await query
        .OrderBy(x => x.ContentKey)
        .ThenBy(x => x.LanguageCode)
        .Take(500)
        .ToListAsync(cancellationToken);

    // Get all POIs to map names
    var pois = await dbContext.Pois.AsNoTracking().ToDictionaryAsync(p => p.Id, cancellationToken);

    var result = translations.Select(t =>
    {
        var poiName = (string?)null;
        if (t.ContentKey.StartsWith("poi."))
        {
            var parts = t.ContentKey.Split('.');
            if (parts.Length >= 2 && Guid.TryParse(parts[1], out var poiId) && pois.TryGetValue(poiId, out var poi))
            {
                poiName = poi.Name;
            }
        }
        return new
        {
            t.Id,
            t.ContentKey,
            t.LanguageCode,
            t.Value,
            poiName
        };
    });

    return Results.Ok(result);
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
    username = session.User?.Username,
    session.PoiId,
    poiName = session.Poi?.Name,
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
    username = subscription.User?.Username,
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

string HashPassword(string password)
{
    // Simple hash for demo purposes - in production use BCrypt or similar
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var bytes = System.Text.Encoding.UTF8.GetBytes(password + "VinhKhanhSalt2024");
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}

bool VerifyPassword(string password, string hash)
{
    return HashPassword(password) == hash;
}

object MapVisitSession(VisitSession visit) => new
{
    visit.Id,
    visit.UserId,
    username = visit.User?.Username,
    visit.PoiId,
    poiName = visit.Poi?.Name,
    visit.VisitedAtUtc,
    visit.LeftAtUtc,
    visit.DurationSeconds,
    triggerSource = visit.TriggerSource.ToString(),
    pageSource = visit.PageSource.ToString(),
    visit.Latitude,
    visit.Longitude,
    visit.ListenedToAudio,
    visit.ListeningSessionCount,
    visit.TotalListenDurationSeconds
};

object MapTourViewSession(TourViewSession tourView) => new
{
    tourView.Id,
    tourView.UserId,
    tourView.TourId,
    tourName = tourView.Tour?.Name,
    tourView.ViewedAtUtc,
    tourView.ClosedAtUtc,
    tourView.DurationSeconds,
    tourView.PoiVisitedCount,
    tourView.AudioListenedCount
};

object MapGeofenceEvent(PoiGeofenceEvent geofenceEvent) => new
{
    geofenceEvent.Id,
    geofenceEvent.UserId,
    geofenceEvent.PoiId,
    poiName = geofenceEvent.Poi?.Name,
    geofenceEvent.OccurredAtUtc,
    eventType = geofenceEvent.EventType.ToString(),
    geofenceEvent.Latitude,
    geofenceEvent.Longitude,
    geofenceEvent.DistanceFromCenterMeters
};

public sealed record StartVisitRequest(
    Guid UserId,
    Guid PoiId,
    string TriggerSource = "Map",
    string PageSource = "Map",
    double? Latitude = null,
    double? Longitude = null,
    string? AnonymousRef = null);

public sealed record UpdateVisitAudioRequest(
    int ListeningSessionCount,
    int TotalListenDurationSeconds);

public sealed record StartTourViewRequest(
    Guid UserId,
    string? AnonymousRef = null);

public sealed record EndTourViewRequest(
    int PoiVisitedCount,
    int AudioListenedCount);

public sealed record RecordGeofenceEventRequest(
    Guid UserId,
    Guid PoiId,
    string EventType,
    double Latitude,
    double Longitude,
    double DistanceFromCenterMeters,
    string? AnonymousRef = null);

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
    string? MapLink = null,
    Guid? ManagerUserId = null);

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
    string? Description = null,
    Guid? ShopId = null);

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
    string Username,
    string PreferredLanguage = "vi",
    string? Password = null)
{
    // Alias for backward compatibility with mobile app
    public string? ExternalRef => Username;
}

public sealed record RegisterRequest(
    string Username,
    string Password,
    string PreferredLanguage = "vi");

public sealed record ChangePasswordRequest(
    string Username,
    string CurrentPassword,
    string NewPassword);

public sealed record RegisterShopRequest(
    string Name,
    string? Address = null,
    string? Description = null,
    string? MapLink = null,
    double? Latitude = null,
    double? Longitude = null);

public sealed record CreateShopRequest(
    string Name,
    Guid? ManagerUserId = null,
    string? Address = null,
    string? Description = null,
    string? MapLink = null,
    double? Latitude = null,
    double? Longitude = null);

public sealed record CreateShopManagerRequest(
    string Username,
    string PreferredLanguage = "vi",
    string? Password = null);

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
