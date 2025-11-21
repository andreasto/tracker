using Microsoft.AspNetCore.Mvc;
using tracker.App.Services;

namespace tracker.App.Controllers;

[ApiController]
[Route("[controller]")]
public class CleanupController : ControllerBase
{
    private readonly ILogger<CleanupController> _logger;
    private readonly IConfiguration _configuration;

    public CleanupController(ILogger<CleanupController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Get cleanup statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var connectionString = _configuration.GetConnectionString("PostgreSql");
        if (string.IsNullOrEmpty(connectionString))
        {
            return BadRequest(new { message = "PostgreSQL connection string not configured" });
        }

        var cleanupService = new DatabaseCleanupService(
            connectionString, 
            HttpContext.RequestServices.GetRequiredService<ILogger<DatabaseCleanupService>>());
        
        var stats = await cleanupService.GetStatsAsync();
        
        return Ok(new
        {
            stats,
            warning = "Use cleanup endpoints carefully - they permanently delete data!"
        });
    }

    /// <summary>
    /// List all User persistence IDs
    /// </summary>
    [HttpGet("persistence-ids")]
    public async Task<IActionResult> GetPersistenceIds()
    {
        var connectionString = _configuration.GetConnectionString("PostgreSql");
        if (string.IsNullOrEmpty(connectionString))
        {
            return BadRequest(new { message = "PostgreSQL connection string not configured" });
        }

        var cleanupService = new DatabaseCleanupService(
            connectionString, 
            HttpContext.RequestServices.GetRequiredService<ILogger<DatabaseCleanupService>>());
        
        var ids = await cleanupService.GetUserPersistenceIdsAsync();
        
        return Ok(new { persistenceIds = ids, count = ids.Count });
    }

    /// <summary>
    /// Clear all User events (fresh start for users)
    /// WARNING: This deletes all user event data!
    /// </summary>
    [HttpPost("clear-user-events")]
    public async Task<IActionResult> ClearUserEvents([FromQuery] bool confirm = false)
    {
        if (!confirm)
        {
            return BadRequest(new 
            { 
                message = "This will delete ALL user events. Add ?confirm=true to proceed.",
                hint = "GET /Cleanup/stats to see what will be deleted"
            });
        }

        var connectionString = _configuration.GetConnectionString("PostgreSql");
        if (string.IsNullOrEmpty(connectionString))
        {
            return BadRequest(new { message = "PostgreSQL connection string not configured" });
        }

        var cleanupService = new DatabaseCleanupService(
            connectionString, 
            HttpContext.RequestServices.GetRequiredService<ILogger<DatabaseCleanupService>>());
        
        var deleted = await cleanupService.ClearUserEventsAsync();
        
        return Ok(new { message = "User events cleared", eventsDeleted = deleted });
    }

    /// <summary>
    /// Clear all User snapshots
    /// WARNING: This deletes all user snapshot data!
    /// </summary>
    [HttpPost("clear-user-snapshots")]
    public async Task<IActionResult> ClearUserSnapshots([FromQuery] bool confirm = false)
    {
        if (!confirm)
        {
            return BadRequest(new 
            { 
                message = "This will delete ALL user snapshots. Add ?confirm=true to proceed."
            });
        }

        var connectionString = _configuration.GetConnectionString("PostgreSql");
        if (string.IsNullOrEmpty(connectionString))
        {
            return BadRequest(new { message = "PostgreSQL connection string not configured" });
        }

        var cleanupService = new DatabaseCleanupService(
            connectionString, 
            HttpContext.RequestServices.GetRequiredService<ILogger<DatabaseCleanupService>>());
        
        var deleted = await cleanupService.ClearUserSnapshotsAsync();
        
        return Ok(new { message = "User snapshots cleared", snapshotsDeleted = deleted });
    }

    /// <summary>
    /// Clear all email lookup mappings
    /// </summary>
    [HttpPost("clear-email-lookups")]
    public async Task<IActionResult> ClearEmailLookups([FromQuery] bool confirm = false)
    {
        if (!confirm)
        {
            return BadRequest(new 
            { 
                message = "This will delete ALL email lookup mappings. Add ?confirm=true to proceed."
            });
        }

        var connectionString = _configuration.GetConnectionString("PostgreSql");
        if (string.IsNullOrEmpty(connectionString))
        {
            return BadRequest(new { message = "PostgreSQL connection string not configured" });
        }

        var cleanupService = new DatabaseCleanupService(
            connectionString, 
            HttpContext.RequestServices.GetRequiredService<ILogger<DatabaseCleanupService>>());
        
        var deleted = await cleanupService.ClearEmailLookupsAsync();
        
        return Ok(new { message = "Email lookups cleared", emailLookupsDeleted = deleted });
    }

    /// <summary>
    /// Remove duplicate events (keeps latest per persistence ID)
    /// </summary>
    [HttpPost("remove-duplicates")]
    public async Task<IActionResult> RemoveDuplicates([FromQuery] bool confirm = false)
    {
        if (!confirm)
        {
            return BadRequest(new 
            { 
                message = "This will remove duplicate events (keeping latest). Add ?confirm=true to proceed."
            });
        }

        var connectionString = _configuration.GetConnectionString("PostgreSql");
        if (string.IsNullOrEmpty(connectionString))
        {
            return BadRequest(new { message = "PostgreSQL connection string not configured" });
        }

        var cleanupService = new DatabaseCleanupService(
            connectionString, 
            HttpContext.RequestServices.GetRequiredService<ILogger<DatabaseCleanupService>>());
        
        var deleted = await cleanupService.RemoveDuplicateUserEventsAsync();
        
        return Ok(new { message = "Duplicates removed", duplicatesDeleted = deleted });
    }

    /// <summary>
    /// NUCLEAR OPTION: Clear ALL persistence data (use with extreme caution!)
    /// WARNING: This deletes EVERYTHING - all actors, all events, all snapshots!
    /// </summary>
    [HttpPost("nuclear-clean")]
    public async Task<IActionResult> NuclearClean([FromQuery] string secret = "")
    {
        // Require a secret to prevent accidental deletion
        if (secret != "CONFIRM_DELETE_EVERYTHING")
        {
            return BadRequest(new 
            { 
                message = "This will delete ALL persistence data (events, snapshots, email lookups).",
                hint = "Add ?secret=CONFIRM_DELETE_EVERYTHING to proceed (you must type it exactly)",
                warning = "⚠️ THIS CANNOT BE UNDONE! ⚠️"
            });
        }

        var connectionString = _configuration.GetConnectionString("PostgreSql");
        if (string.IsNullOrEmpty(connectionString))
        {
            return BadRequest(new { message = "PostgreSQL connection string not configured" });
        }

        var cleanupService = new DatabaseCleanupService(
            connectionString, 
            HttpContext.RequestServices.GetRequiredService<ILogger<DatabaseCleanupService>>());
        
        var result = await cleanupService.NuclearCleanAsync();
        
        return Ok(new 
        { 
            message = "Nuclear clean completed - database is empty", 
            result,
            warning = "All data has been permanently deleted"
        });
    }

    /// <summary>
    /// Check if a specific user exists by email
    /// </summary>
    [HttpGet("user/{email}")]
    public async Task<IActionResult> CheckUser(string email)
    {
        var connectionString = _configuration.GetConnectionString("PostgreSql");
        if (string.IsNullOrEmpty(connectionString))
        {
            return BadRequest(new { message = "PostgreSQL connection string not configured" });
        }

        var cleanupService = new DatabaseCleanupService(
            connectionString, 
            HttpContext.RequestServices.GetRequiredService<ILogger<DatabaseCleanupService>>());
        
        var emailLookupService = HttpContext.RequestServices.GetRequiredService<IEmailLookupService>();
        
        var info = await cleanupService.CheckUserExistsByEmailAsync(email, emailLookupService);
        
        return Ok(info);
    }

    /// <summary>
    /// Delete a specific user by email address
    /// WARNING: This permanently deletes all data for this user!
    /// </summary>
    [HttpDelete("user/{email}")]
    public async Task<IActionResult> DeleteUser(string email, [FromQuery] bool confirm = false)
    {
        if (!confirm)
        {
            return BadRequest(new 
            { 
                message = $"This will permanently delete all data for user: {email}. Add ?confirm=true to proceed.",
                hint = "GET /Cleanup/user/{email} to see what will be deleted"
            });
        }

        var connectionString = _configuration.GetConnectionString("PostgreSql");
        if (string.IsNullOrEmpty(connectionString))
        {
            return BadRequest(new { message = "PostgreSQL connection string not configured" });
        }

        var cleanupService = new DatabaseCleanupService(
            connectionString, 
            HttpContext.RequestServices.GetRequiredService<ILogger<DatabaseCleanupService>>());
        
        var emailLookupService = HttpContext.RequestServices.GetRequiredService<IEmailLookupService>();
        
        var result = await cleanupService.DeleteUserByEmailAsync(email, emailLookupService);
        
        if (!result.Success)
        {
            return NotFound(result);
        }
        
        return Ok(result);
    }

    /// <summary>
    /// Complete user cleanup workflow (recommended)
    /// Clears user events, snapshots, and email lookups in one go
    /// </summary>
    [HttpPost("clean-all-users")]
    public async Task<IActionResult> CleanAllUsers([FromQuery] bool confirm = false)
    {
        if (!confirm)
        {
            return BadRequest(new 
            { 
                message = "This will delete ALL user data (events, snapshots, email lookups). Add ?confirm=true to proceed.",
                hint = "GET /Cleanup/stats to see what will be deleted"
            });
        }

        var connectionString = _configuration.GetConnectionString("PostgreSql");
        if (string.IsNullOrEmpty(connectionString))
        {
            return BadRequest(new { message = "PostgreSQL connection string not configured" });
        }

        var cleanupService = new DatabaseCleanupService(
            connectionString, 
            HttpContext.RequestServices.GetRequiredService<ILogger<DatabaseCleanupService>>());
        
        var eventsDeleted = await cleanupService.ClearUserEventsAsync();
        var snapshotsDeleted = await cleanupService.ClearUserSnapshotsAsync();
        var emailsDeleted = await cleanupService.ClearEmailLookupsAsync();
        
        return Ok(new 
        { 
            message = "All user data cleared", 
            eventsDeleted,
            snapshotsDeleted,
            emailLookupsDeleted = emailsDeleted,
            note = "You can now register users fresh with UUID-based userIds"
        });
    }
}

