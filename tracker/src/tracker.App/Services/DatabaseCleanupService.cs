using Dapper;
using Npgsql;

namespace tracker.App.Services;

/// <summary>
/// Utility service for cleaning up old/duplicate event journal data
/// </summary>
public class DatabaseCleanupService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseCleanupService> _logger;

    public DatabaseCleanupService(string connectionString, ILogger<DatabaseCleanupService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    /// <summary>
    /// Delete all User events from event journal (fresh start for users)
    /// </summary>
    public async Task<int> ClearUserEventsAsync()
    {
        _logger.LogWarning("Clearing all User events from event journal...");
        
        using var connection = new NpgsqlConnection(_connectionString);
        
        var deletedCount = await connection.ExecuteAsync(@"
            DELETE FROM event_journal 
            WHERE persistence_id LIKE 'User_%'
        ");
        
        _logger.LogInformation("Deleted {Count} User events from event_journal", deletedCount);
        return deletedCount;
    }

    /// <summary>
    /// Delete all User snapshots
    /// </summary>
    public async Task<int> ClearUserSnapshotsAsync()
    {
        _logger.LogWarning("Clearing all User snapshots...");
        
        using var connection = new NpgsqlConnection(_connectionString);
        
        var deletedCount = await connection.ExecuteAsync(@"
            DELETE FROM snapshot 
            WHERE persistence_id LIKE 'User_%'
        ");
        
        _logger.LogInformation("Deleted {Count} User snapshots", deletedCount);
        return deletedCount;
    }

    /// <summary>
    /// Clear all email lookup mappings
    /// </summary>
    public async Task<int> ClearEmailLookupsAsync()
    {
        _logger.LogWarning("Clearing all email lookup mappings...");
        
        using var connection = new NpgsqlConnection(_connectionString);
        
        try
        {
            var deletedCount = await connection.ExecuteAsync("DELETE FROM email_lookup");
            _logger.LogInformation("Deleted {Count} email lookup entries", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email lookup table may not exist yet");
            return 0;
        }
    }

    /// <summary>
    /// Delete duplicate persistence IDs (keeps the latest)
    /// </summary>
    public async Task<int> RemoveDuplicateUserEventsAsync()
    {
        _logger.LogWarning("Removing duplicate User events (keeping latest)...");
        
        using var connection = new NpgsqlConnection(_connectionString);
        
        var deletedCount = await connection.ExecuteAsync(@"
            DELETE FROM event_journal 
            WHERE event_id IN (
                SELECT event_id 
                FROM (
                    SELECT event_id, 
                           persistence_id,
                           ROW_NUMBER() OVER (
                               PARTITION BY persistence_id 
                               ORDER BY sequence_number DESC
                           ) AS rn
                    FROM event_journal 
                    WHERE persistence_id LIKE 'User_%'
                ) sub 
                WHERE rn > 1
            )
        ");
        
        _logger.LogInformation("Removed {Count} duplicate User events", deletedCount);
        return deletedCount;
    }

    /// <summary>
    /// Get statistics about current data
    /// </summary>
    public async Task<CleanupStats> GetStatsAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        var stats = new CleanupStats();

        try
        {
            // Count user events
            stats.UserEventCount = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM event_journal WHERE persistence_id LIKE 'User_%'
            ");

            // Count user snapshots
            stats.UserSnapshotCount = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM snapshot WHERE persistence_id LIKE 'User_%'
            ");

            // Count email lookups
            try
            {
                stats.EmailLookupCount = await connection.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM email_lookup
                ");
            }
            catch
            {
                stats.EmailLookupCount = 0;
            }

            // Get unique persistence IDs
            stats.UniquePersistenceIds = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(DISTINCT persistence_id) 
                FROM event_journal 
                WHERE persistence_id LIKE 'User_%'
            ");

            // Check for duplicates
            stats.DuplicatePersistenceIds = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM (
                    SELECT persistence_id 
                    FROM event_journal 
                    WHERE persistence_id LIKE 'User_%'
                    GROUP BY persistence_id 
                    HAVING COUNT(*) > 1
                ) sub
            ");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stats");
        }

        return stats;
    }

    /// <summary>
    /// List all User persistence IDs
    /// </summary>
    public async Task<List<string>> GetUserPersistenceIdsAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        var ids = await connection.QueryAsync<string>(@"
            SELECT DISTINCT persistence_id 
            FROM event_journal 
            WHERE persistence_id LIKE 'User_%'
            ORDER BY persistence_id
        ");
        
        return ids.ToList();
    }

    /// <summary>
    /// Delete a specific user by email address
    /// </summary>
    public async Task<UserDeletionResult> DeleteUserByEmailAsync(string email, IEmailLookupService emailLookupService)
    {
        var normalizedEmail = email.ToLowerInvariant();
        _logger.LogWarning("Attempting to delete user with email: {Email}", normalizedEmail);
        
        var result = new UserDeletionResult { Email = normalizedEmail };
        
        using var connection = new NpgsqlConnection(_connectionString);
        
        // 1. Get userId from email
        var userId = await emailLookupService.GetUserIdByEmailAsync(normalizedEmail);
        
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("No user found for email: {Email}", normalizedEmail);
            result.Success = false;
            result.Message = "User not found";
            return result;
        }
        
        result.UserId = userId;
        var persistenceId = $"User_{userId}";
        
        // 2. Delete events from event_journal
        result.EventsDeleted = await connection.ExecuteAsync(@"
            DELETE FROM event_journal 
            WHERE persistence_id = @PersistenceId
        ", new { PersistenceId = persistenceId });
        
        // 3. Delete snapshots
        result.SnapshotsDeleted = await connection.ExecuteAsync(@"
            DELETE FROM snapshot 
            WHERE persistence_id = @PersistenceId
        ", new { PersistenceId = persistenceId });
        
        // 4. Delete email lookup mapping
        try
        {
            result.EmailLookupDeleted = await connection.ExecuteAsync(@"
                DELETE FROM email_lookup 
                WHERE email = @Email
            ", new { Email = normalizedEmail }) > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete email lookup for {Email}", normalizedEmail);
        }
        
        result.Success = true;
        result.Message = "User deleted successfully";
        
        _logger.LogInformation(
            "Deleted user {Email}: {Events} events, {Snapshots} snapshots, email lookup: {EmailDeleted}",
            normalizedEmail, result.EventsDeleted, result.SnapshotsDeleted, result.EmailLookupDeleted);
        
        return result;
    }

    /// <summary>
    /// Check if a user exists by email
    /// </summary>
    public async Task<UserExistenceInfo> CheckUserExistsByEmailAsync(string email, IEmailLookupService emailLookupService)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var info = new UserExistenceInfo { Email = normalizedEmail };
        
        using var connection = new NpgsqlConnection(_connectionString);
        
        // Check email lookup
        var userId = await emailLookupService.GetUserIdByEmailAsync(normalizedEmail);
        if (!string.IsNullOrEmpty(userId))
        {
            info.UserId = userId;
            info.HasEmailLookup = true;
            
            var persistenceId = $"User_{userId}";
            
            // Check events
            info.EventCount = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM event_journal 
                WHERE persistence_id = @PersistenceId
            ", new { PersistenceId = persistenceId });
            
            // Check snapshots
            info.SnapshotCount = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM snapshot 
                WHERE persistence_id = @PersistenceId
            ", new { PersistenceId = persistenceId });
            
            info.Exists = info.EventCount > 0 || info.SnapshotCount > 0 || info.HasEmailLookup;
        }
        
        return info;
    }

    /// <summary>
    /// Nuclear option: Clear ALL Akka persistence data (use with extreme caution!)
    /// </summary>
    public async Task<CleanupResult> NuclearCleanAsync()
    {
        _logger.LogWarning("!!! NUCLEAR CLEAN - Deleting ALL Akka persistence data !!!");
        
        var result = new CleanupResult();
        
        using var connection = new NpgsqlConnection(_connectionString);
        
        // Clear event journal
        result.EventsDeleted = await connection.ExecuteAsync("DELETE FROM event_journal");
        _logger.LogInformation("Deleted {Count} events from event_journal", result.EventsDeleted);
        
        // Clear snapshots
        result.SnapshotsDeleted = await connection.ExecuteAsync("DELETE FROM snapshot");
        _logger.LogInformation("Deleted {Count} snapshots", result.SnapshotsDeleted);
        
        // Clear email lookups
        try
        {
            result.EmailLookupsDeleted = await connection.ExecuteAsync("DELETE FROM email_lookup");
            _logger.LogInformation("Deleted {Count} email lookups", result.EmailLookupsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not clear email_lookup table");
        }
        
        _logger.LogWarning("Nuclear clean completed. Database is now empty.");
        return result;
    }
}

public record CleanupStats
{
    public int UserEventCount { get; set; }
    public int UserSnapshotCount { get; set; }
    public int EmailLookupCount { get; set; }
    public int UniquePersistenceIds { get; set; }
    public int DuplicatePersistenceIds { get; set; }
}

public record CleanupResult
{
    public int EventsDeleted { get; set; }
    public int SnapshotsDeleted { get; set; }
    public int EmailLookupsDeleted { get; set; }
}

public record UserDeletionResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public int EventsDeleted { get; set; }
    public int SnapshotsDeleted { get; set; }
    public bool EmailLookupDeleted { get; set; }
}

public record UserExistenceInfo
{
    public string Email { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public bool Exists { get; set; }
    public bool HasEmailLookup { get; set; }
    public int EventCount { get; set; }
    public int SnapshotCount { get; set; }
}

