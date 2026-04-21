using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        await ApplySchemaMigrationsAsync(cancellationToken);

        await _dataSeeder.SeedAsync(cancellationToken);
        _logger.LogInformation("Ensured seed data is up to date (idempotent).");
    }

    private async Task ApplySchemaMigrationsAsync(CancellationToken cancellationToken)
    {
        if (!_dbContext.Database.IsSqlite())
        {
            return;
        }

        await EnsurePriorityColumnAsync(cancellationToken);
        await EnsureImageUrlColumnAsync(cancellationToken);
        await EnsureMapLinkColumnAsync(cancellationToken);
    }

    private async Task EnsurePriorityColumnAsync(CancellationToken cancellationToken)
    {
        const string columnName = "Priority";
        if (await PoiColumnExistsAsync(columnName, cancellationToken))
        {
            return;
        }

        await _dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"Pois\" ADD COLUMN \"Priority\" INTEGER NOT NULL DEFAULT 0;",
            cancellationToken);

        _logger.LogInformation("Applied schema migration: added Pois.{ColumnName}", columnName);
    }

    private async Task EnsureImageUrlColumnAsync(CancellationToken cancellationToken)
    {
        const string columnName = "ImageUrl";
        if (await PoiColumnExistsAsync(columnName, cancellationToken))
        {
            return;
        }

        await _dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"Pois\" ADD COLUMN \"ImageUrl\" TEXT NULL;",
            cancellationToken);

        _logger.LogInformation("Applied schema migration: added Pois.{ColumnName}", columnName);
    }

    private async Task EnsureMapLinkColumnAsync(CancellationToken cancellationToken)
    {
        const string columnName = "MapLink";
        if (await PoiColumnExistsAsync(columnName, cancellationToken))
        {
            return;
        }

        await _dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"Pois\" ADD COLUMN \"MapLink\" TEXT NULL;",
            cancellationToken);

        _logger.LogInformation("Applied schema migration: added Pois.{ColumnName}", columnName);
    }

    private async Task<bool> PoiColumnExistsAsync(string columnName, CancellationToken cancellationToken)
    {
        await using var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('Pois');";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var existing = reader.GetString(1);
            if (string.Equals(existing, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
