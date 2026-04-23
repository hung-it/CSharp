using Microsoft.Extensions.DependencyInjection;
using VinhKhanhAudioGuide.Backend.Application.Services;

namespace VinhKhanhAudioGuide.Backend.Infrastructure;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IPoiService, PoiService>();
        services.AddScoped<IPoiAuthorizationService, PoiAuthorizationService>();
        services.AddScoped<ITourService, TourService>();
        services.AddScoped<IListeningSessionService, ListeningSessionService>();
        services.AddScoped<IVisitTrackingService, VisitTrackingService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IContentTranslationService, ContentTranslationService>();
        services.AddScoped<IRouteTrackingService, RouteTrackingService>();
        services.AddScoped<IQrPlaybackService, QrPlaybackService>();
        services.AddScoped<IContentSyncService, ContentSyncService>();
        services.AddSingleton<IGeofenceService, GeofenceService>();
        services.AddSingleton<INarrationQueueService, NarrationQueueService>();
        
        // Shop Management Services
        services.AddScoped<IShopManagementService, ShopManagementService>();
        services.AddScoped<IShopAnalyticsService, ShopAnalyticsService>();
        services.AddScoped<IShopQRCodeService, ShopQRCodeService>();
        services.AddScoped<IShopAudioService, ShopAudioService>();
        services.AddScoped<IShopAuthorizationService, ShopAuthorizationService>();
        
        return services;
    }
}
