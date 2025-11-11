using FluentMigrator.Runner;

namespace tracker.App.Configuration;

/// <summary>
/// Extension methods for configuring FluentMigrator
/// </summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Add FluentMigrator services to the service collection
    /// </summary>
    public static IServiceCollection AddDatabaseMigrations(
        this IServiceCollection services, 
        string connectionString)
    {
        services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(Program).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .AddScoped<MigrationService>();

        return services;
    }

    /// <summary>
    /// Run database migrations
    /// </summary>
    public static IApplicationBuilder UseDatabaseMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var migrationService = scope.ServiceProvider.GetRequiredService<MigrationService>();
        migrationService.MigrateUp();
        return app;
    }
}

