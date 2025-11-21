using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace tracker.App.Services;

public record RefreshToken(
    string Token,
    string UserId,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    string? ReplacedByToken = null,
    bool IsRevoked = false);

public interface IRefreshTokenService
{
    Task<RefreshToken> GenerateRefreshTokenAsync(string userId);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token, string? replacedByToken = null);
    Task<bool> IsValidRefreshTokenAsync(string token);
    Task CleanupExpiredTokensAsync();
}

/// <summary>
/// In-memory refresh token store. For production, use a distributed cache or database.
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    private readonly ConcurrentDictionary<string, RefreshToken> _tokens = new();
    private readonly IConfiguration _configuration;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(IConfiguration configuration, ILogger<RefreshTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<RefreshToken> GenerateRefreshTokenAsync(string userId)
    {
        var token = GenerateSecureToken();
        var expiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");
        
        var refreshToken = new RefreshToken(
            Token: token,
            UserId: userId,
            ExpiresAt: DateTime.UtcNow.AddDays(expiryDays),
            CreatedAt: DateTime.UtcNow
        );

        _tokens[token] = refreshToken;
        
        _logger.LogInformation("Generated refresh token for user {UserId}", userId);
        
        return Task.FromResult(refreshToken);
    }

    public Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        _tokens.TryGetValue(token, out var refreshToken);
        return Task.FromResult(refreshToken);
    }

    public Task RevokeRefreshTokenAsync(string token, string? replacedByToken = null)
    {
        if (_tokens.TryGetValue(token, out var existingToken))
        {
            var revokedToken = existingToken with 
            { 
                IsRevoked = true,
                ReplacedByToken = replacedByToken
            };
            
            _tokens[token] = revokedToken;
            
            _logger.LogInformation("Revoked refresh token for user {UserId}", existingToken.UserId);
        }

        return Task.CompletedTask;
    }

    public async Task<bool> IsValidRefreshTokenAsync(string token)
    {
        var refreshToken = await GetRefreshTokenAsync(token);
        
        if (refreshToken == null)
            return false;

        if (refreshToken.IsRevoked)
            return false;

        if (refreshToken.ExpiresAt < DateTime.UtcNow)
            return false;

        return true;
    }

    public Task CleanupExpiredTokensAsync()
    {
        var expiredTokens = _tokens
            .Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var token in expiredTokens)
        {
            _tokens.TryRemove(token, out _);
        }

        if (expiredTokens.Any())
        {
            _logger.LogInformation("Cleaned up {Count} expired refresh tokens", expiredTokens.Count);
        }

        return Task.CompletedTask;
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

