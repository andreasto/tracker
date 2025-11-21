using System.Collections.Concurrent;

namespace tracker.App.Services;

public interface IEmailLookupService
{
    Task<string?> GetUserIdByEmailAsync(string email);
    Task StoreEmailMappingAsync(string email, string userId);
    Task<bool> EmailExistsAsync(string email);
}

/// <summary>
/// In-memory email to userId lookup service.
/// 
/// WARNING: All mappings are LOST on application restart!
/// 
/// Use this for:
/// - Development/testing
/// - Single-instance deployments where restarts are rare
/// 
/// For production, use PostgreSqlEmailLookupService instead.
/// </summary>
public class EmailLookupService : IEmailLookupService
{
    private readonly ConcurrentDictionary<string, string> _emailToUserId = new();
    private readonly ILogger<EmailLookupService> _logger;

    public EmailLookupService(ILogger<EmailLookupService> logger)
    {
        _logger = logger;
    }

    public Task<string?> GetUserIdByEmailAsync(string email)
    {
        var normalizedEmail = email.ToLowerInvariant();
        _emailToUserId.TryGetValue(normalizedEmail, out var userId);
        return Task.FromResult(userId);
    }

    public Task StoreEmailMappingAsync(string email, string userId)
    {
        var normalizedEmail = email.ToLowerInvariant();
        _emailToUserId[normalizedEmail] = userId;
        _logger.LogInformation("Stored email mapping: {Email} -> {UserId}", normalizedEmail, userId);
        return Task.CompletedTask;
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        var normalizedEmail = email.ToLowerInvariant();
        return Task.FromResult(_emailToUserId.ContainsKey(normalizedEmail));
    }
}

