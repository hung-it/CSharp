using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VinhKhanhAudioGuide.Backend.Domain.Entities;
using VinhKhanhAudioGuide.Backend.Persistence;

namespace VinhKhanhAudioGuide.Backend.Infrastructure;

internal sealed class DatabaseInitializer(
    AudioGuideDbContext dbContext,
    IDataSeeder dataSeeder,
    ILogger<DatabaseInitializer> logger) : IDatabaseInitializer
{
    private readonly AudioGuideDbContext _dbContext = dbContext;
    private readonly IDataSeeder _dataSeeder = dataSeeder;
    private readonly ILogger<DatabaseInitializer> _logger = logger;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (await _dbContext.FeatureSegments.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already seeded, skipping initialization.");
            return;
        }

        _dbContext.FeatureSegments.AddRange(
            new FeatureSegment { Code = "basic.poi", Name = "Basic POI" },
            new FeatureSegment { Code = "premium.segment.tour", Name = "Premium Tour Segment" },
            new FeatureSegment { Code = "premium.segment.audio", Name = "Premium Audio Segment" },
            new FeatureSegment { Code = "premium.segment.analytics", Name = "Premium Analytics Segment" });

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded default feature segments.");

        await _dataSeeder.SeedAsync(cancellationToken);
        _logger.LogInformation("Seeded sample data (POI, Tours, Users).");
    }
}
