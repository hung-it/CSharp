using Microsoft.Extensions.DependencyInjection;
using VinhKhanhAudioGuide.Backend.Infrastructure;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Tests.Infrastructure;

public sealed class DatabaseBootstrapTests
{
    [Fact]
    public async Task InitializeAsync_CreatesDatabaseAndSeedsFeatureSegments()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"audio-guide-{Guid.NewGuid():N}.db");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAudioGuideBackend(options => options.DatabasePath = databasePath);

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
        await initializer.InitializeAsync();

        var dbContext = scope.ServiceProvider.GetRequiredService<AudioGuideDbContext>();
        Assert.True(dbContext.FeatureSegments.Count() >= 4);

        await dbContext.Database.EnsureDeletedAsync();
    }
}
