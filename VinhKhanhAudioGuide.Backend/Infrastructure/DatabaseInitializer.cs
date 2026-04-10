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

        await EnsurePoiColumnAsync("Priority", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await EnsurePoiColumnAsync("ImageUrl", "TEXT NULL", cancellationToken);
        await EnsurePoiColumnAsync("MapLink", "TEXT NULL", cancellationToken);
    }

    private async Task EnsurePoiColumnAsync(string columnName, string columnDefinition, CancellationToken cancellationToken)
    {
        if (await PoiColumnExistsAsync(columnName, cancellationToken))
        {
            return;
        }

        await _dbContext.Database.ExecuteSqlRawAsync(
            $"ALTER TABLE \"Pois\" ADD COLUMN \"{columnName}\" {columnDefinition};",
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
