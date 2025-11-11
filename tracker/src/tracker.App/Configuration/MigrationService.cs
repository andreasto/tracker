using FluentMigrator.Runner;

namespace tracker.App.Configuration;

/// <summary>
/// Service to run database migrations on application startup
/// </summary>
public class MigrationService
{
    private readonly IMigrationRunner _migrationRunner;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(IMigrationRunner migrationRunner, ILogger<MigrationService> logger)
    {
        _migrationRunner = migrationRunner;
        _logger = logger;
    }

    /// <summary>
    /// Run all pending migrations
    /// </summary>
    public void MigrateUp()
    {
        try
        {
            _logger.LogInformation("Starting database migrations...");
            _migrationRunner.MigrateUp();
            _logger.LogInformation("Database migrations completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running database migrations");
            throw;
        }
    }

    /// <summary>
    /// Rollback the last migration
    /// </summary>
    public void MigrateDown()
    {
        try
        {
            _logger.LogInformation("Rolling back last migration...");
            _migrationRunner.MigrateDown(1);
            _logger.LogInformation("Migration rollback completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back migration");
            throw;
        }
    }

    /// <summary>
    /// Migrate to a specific version
    /// </summary>
    public void MigrateTo(long version)
    {
        try
        {
            _logger.LogInformation("Migrating to version {Version}...", version);
            _migrationRunner.MigrateUp(version);
            _logger.LogInformation("Migration to version {Version} completed successfully", version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating to version {Version}", version);
            throw;
        }
    }

    /// <summary>
    /// List all migrations and their status
    /// </summary>
    public void ListMigrations()
    {
        _logger.LogInformation("Listing all migrations:");
        _migrationRunner.ListMigrations();
    }
}

