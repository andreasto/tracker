using Dapper;
using Npgsql;

namespace tracker.App.Services;

/// <summary>
/// PostgreSQL-backed email to userId lookup service.
/// Persists mappings across restarts.
/// </summary>
public class PostgreSqlEmailLookupService : IEmailLookupService
{
    private readonly string _connectionString;
    private readonly ILogger<PostgreSqlEmailLookupService> _logger;

    public PostgreSqlEmailLookupService(string connectionString, ILogger<PostgreSqlEmailLookupService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<string?> GetUserIdByEmailAsync(string email)
    {
        var normalizedEmail = email.ToLowerInvariant();
        
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = "SELECT user_id FROM email_lookup WHERE email = @Email";
            var userId = await connection.QuerySingleOrDefaultAsync<string>(query, new { Email = normalizedEmail });
            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lookup userId for email: {Email}", normalizedEmail);
            return null;
        }
    }

    public async Task StoreEmailMappingAsync(string email, string userId)
    {
        var normalizedEmail = email.ToLowerInvariant();
        
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                INSERT INTO email_lookup (email, user_id, created_at, updated_at)
                VALUES (@Email, @UserId, NOW(), NOW())
                ON CONFLICT (email) 
                DO UPDATE SET user_id = @UserId, updated_at = NOW()";
            
            await connection.ExecuteAsync(query, new { Email = normalizedEmail, UserId = userId });
            _logger.LogInformation("Stored email mapping: {Email} -> {UserId}", normalizedEmail, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store email mapping: {Email} -> {UserId}", normalizedEmail, userId);
            throw;
        }
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        var normalizedEmail = email.ToLowerInvariant();
        
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = "SELECT EXISTS(SELECT 1 FROM email_lookup WHERE email = @Email)";
            var exists = await connection.QuerySingleAsync<bool>(query, new { Email = normalizedEmail });
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if email exists: {Email}", normalizedEmail);
            return false;
        }
    }
}

