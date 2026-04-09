using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Infrastructure;

public static class BackendServiceCollectionExtensions
{
    public static IServiceCollection AddAudioGuideBackend(
        this IServiceCollection services,
        Action<BackendOptions>? configure = null)
    {
        services.AddOptions<BackendOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddDbContext<AudioGuideDbContext>((serviceProvider, options) =>
        {
            var backendOptions = serviceProvider.GetRequiredService<IOptions<BackendOptions>>().Value;
            if (string.IsNullOrWhiteSpace(backendOptions.DatabasePath))
            {
                throw new InvalidOperationException("BackendOptions.DatabasePath must be configured.");
            }

            options.UseSqlite($"Data Source={backendOptions.DatabasePath}");
        });

        services.AddScoped<IDataSeeder, DataSeeder>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddApplicationServices();
        return services;
    }
}
