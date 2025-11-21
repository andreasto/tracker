using Akka.Actor;
using Akka.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tracker.App.Actors;
using tracker.App.Services;
using tracker.Domain.User;

namespace tracker.App.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly IActorRef _userActor;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IEmailLookupService _emailLookupService;

    public UserController(
        ILogger<UserController> logger, 
        IRequiredActor<UserActor> userActor, 
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        IEmailLookupService emailLookupService)
    {
        _logger = logger;
        _userActor = userActor.ActorRef;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _emailLookupService = emailLookupService;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userActor.Ask<User>(new FetchUser(userId), TimeSpan.FromSeconds(5));
        return Ok(user);
    }

    [HttpGet("{userId}")]
    [Authorize]
    public async Task<User> Get(string userId)
    {
        var user = await _userActor.Ask<User>(new FetchUser(userId), TimeSpan.FromSeconds(5));
        return user;
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var userId = await _emailLookupService.GetUserIdByEmailAsync(request.Email);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }
        
        var result = await _userActor.Ask<UserCommandResponse>(
            new AuthenticateCommand(userId, request.Password),
            TimeSpan.FromSeconds(5));
        
        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }
        
        var user = await _userActor.Ask<User>(new FetchUser(userId), TimeSpan.FromSeconds(5));
        
        var accessToken = _jwtService.GenerateToken(user.UserId, user.Email, user.Name);
        
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user.UserId);

        return Ok(new 
        { 
            authenticated = true, 
            userId = user.UserId,
            email = user.Email,
            name = user.Name,
            accessToken,
            refreshToken = refreshToken.Token,
            expiresIn = int.Parse(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:ExpiryInMinutes"] ?? "15") * 60 // in seconds
        });
    }
    
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!await _refreshTokenService.IsValidRefreshTokenAsync(request.RefreshToken))
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }
        
        var refreshToken = await _refreshTokenService.GetRefreshTokenAsync(request.RefreshToken);
        if (refreshToken == null)
        {
            return Unauthorized(new { message = "Refresh token not found" });
        }
        
        var user = await _userActor.Ask<User>(new FetchUser(refreshToken.UserId), TimeSpan.FromSeconds(5));
        
        var newAccessToken = _jwtService.GenerateToken(user.UserId, user.Email, user.Name);
        
        var newRefreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user.UserId);
        await _refreshTokenService.RevokeRefreshTokenAsync(request.RefreshToken, newRefreshToken.Token);

        return Ok(new 
        { 
            accessToken = newAccessToken,
            refreshToken = newRefreshToken.Token,
            expiresIn = int.Parse(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:ExpiryInMinutes"] ?? "15") * 60 // in seconds
        });
    }
    
    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
    {
        await _refreshTokenService.RevokeRefreshTokenAsync(request.RefreshToken);
        return Ok(new { message = "Token revoked successfully" });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
    {
        // Check if email already exists
        if (await _emailLookupService.EmailExistsAsync(request.Email))
        {
            return BadRequest(new { message = "Email already registered" });
        }

        var userId = Guid.NewGuid().ToString();
        
        await _emailLookupService.StoreEmailMappingAsync(request.Email, userId);
        
        var createResult = await _userActor.Ask<UserCommandResponse>(
            new CreateUserCommand(userId, request.Name, request.Email),
            TimeSpan.FromSeconds(5));
        
        if (!createResult.IsSuccess)
        {
            return BadRequest(createResult.ErrorMessage);
        }
        
        if (!string.IsNullOrEmpty(request.Password))
        {
            var passwordResult = await _userActor.Ask<UserCommandResponse>(
                new SetPasswordCommand(userId, request.Password),
                TimeSpan.FromSeconds(5));
            
            if (!passwordResult.IsSuccess)
            {
                return BadRequest(passwordResult.ErrorMessage);
            }
        }
        
        var accessToken = _jwtService.GenerateToken(userId, request.Email, request.Name);
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(userId);

        return Ok(new 
        { 
            userId,
            email = request.Email,
            name = request.Name,
            message = "User registered successfully",
            accessToken,
            refreshToken = refreshToken.Token,
            expiresIn = int.Parse(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:ExpiryInMinutes"] ?? "15") * 60 // in seconds
        });
    }

    [HttpPost("{userId}")]
    public async Task<IActionResult> Post(string userId, [FromBody] CreateUserRequest request)
    {
        if (await _emailLookupService.EmailExistsAsync(request.Email))
        {
            return BadRequest(new { message = "Email already registered" });
        }

        await _emailLookupService.StoreEmailMappingAsync(request.Email, userId);
        
        var createResult = await _userActor.Ask<UserCommandResponse>(
            new CreateUserCommand(userId, request.Name, request.Email),
            TimeSpan.FromSeconds(5));
        
        if (!createResult.IsSuccess)
        {
            return BadRequest(createResult.ErrorMessage);
        }

        // Then set the password if provided
        if (!string.IsNullOrEmpty(request.Password))
        {
            var passwordResult = await _userActor.Ask<UserCommandResponse>(
                new SetPasswordCommand(userId, request.Password),
                TimeSpan.FromSeconds(5));
            
            if (!passwordResult.IsSuccess)
            {
                return BadRequest(passwordResult.ErrorMessage);
            }
        }

        return Ok(createResult.Event);
    }

    [HttpPost("{userId}/password")]
    public async Task<IActionResult> SetPassword(string userId, [FromBody] SetPasswordRequest request)
    {
        var result = await _userActor.Ask<UserCommandResponse>(
            new SetPasswordCommand(userId, request.Password),
            TimeSpan.FromSeconds(5));
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Event);
    }

    [HttpPost("{userId}/authenticate")]
    public async Task<IActionResult> Authenticate(string userId, [FromBody] AuthenticateRequest request)
    {
        var result = await _userActor.Ask<UserCommandResponse>(
            new AuthenticateCommand(userId, request.Password),
            TimeSpan.FromSeconds(5));
        
        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.ErrorMessage });
        }
        
        var user = await _userActor.Ask<User>(new FetchUser(userId), TimeSpan.FromSeconds(5));

        var token = _jwtService.GenerateToken(userId, user.Email, user.Name);
        
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(userId);

        return Ok(new 
        { 
            authenticated = true, 
            userId,
            token,
            refreshToken = refreshToken.Token,
            expiresIn = int.Parse(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:ExpiryInMinutes"] ?? "15") * 60 // in seconds
        });
    }

    [HttpPut("{userId}/name")]
    [Authorize]
    public async Task<IActionResult> UpdateName(string userId, [FromBody] UpdateNameRequest request)
    {
        var result = await _userActor.Ask<UserCommandResponse>(
            new UpdateUserNameCommand(userId, request.Name),
            TimeSpan.FromSeconds(5));
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Event);
    }

    [HttpPut("{userId}/email")]
    [Authorize]
    public async Task<IActionResult> UpdateEmail(string userId, [FromBody] UpdateEmailRequest request)
    {
        var result = await _userActor.Ask<UserCommandResponse>(
            new UpdateUserEmailCommand(userId, request.Email),
            TimeSpan.FromSeconds(5));
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Event);
    }

    [HttpPost("{userId}/questionnaire")]
    [Authorize]
    public async Task<IActionResult> AnswerQuestionnaire(string userId, [FromBody] AnswerQuestionnaireRequest request)
    {
        var result = await _userActor.Ask<UserCommandResponse>(
            new AnswerQuestionnaireCommand(userId, request.Answers),
            TimeSpan.FromSeconds(5));
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Event);
    }

    [HttpPost("{userId}/start-values")]
    [Authorize]
    public async Task<IActionResult> ProvideStartValues(string userId, [FromBody] ProvideStartValuesRequest request)
    {
        var result = await _userActor.Ask<UserCommandResponse>(
            new ProvideStartValuesCommand(userId, request.StartWeight, request.Measurements),
            TimeSpan.FromSeconds(5));
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Event);
    }

    [HttpPost("{userId}/complete-onboarding")]
    [Authorize]
    public async Task<IActionResult> CompleteOnboarding(string userId)
    {
        var result = await _userActor.Ask<UserCommandResponse>(
            new CompleteOnboardingCommand(userId),
            TimeSpan.FromSeconds(5));
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Event);
    }
}

public record CreateUserRequest(string Name, string Email, string? Password = null);
public record UpdateNameRequest(string Name);
public record UpdateEmailRequest(string Email);
public record AnswerQuestionnaireRequest(Dictionary<string, string> Answers);
public record ProvideStartValuesRequest(double StartWeight, Dictionary<string, double> Measurements);
public record SetPasswordRequest(string Password);
public record AuthenticateRequest(string Password);
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record RevokeTokenRequest(string RefreshToken);


