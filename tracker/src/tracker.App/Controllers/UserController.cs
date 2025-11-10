using Akka.Actor;
using Akka.Hosting;
using Microsoft.AspNetCore.Mvc;
using tracker.App.Actors;
using tracker.Domain.User;

namespace tracker.App.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly IActorRef _userActor;

    public UserController(ILogger<UserController> logger, IRequiredActor<UserActor> userActor)
    {
        _logger = logger;
        _userActor = userActor.ActorRef;
    }

    [HttpGet("{userId}")]
    public async Task<User> Get(string userId)
    {
        var user = await _userActor.Ask<User>(new FetchUser(userId), TimeSpan.FromSeconds(5));
        return user;
    }

    [HttpPost("{userId}")]
    public async Task<IActionResult> Post(string userId, [FromBody] CreateUserRequest request)
    {
        var result = await _userActor.Ask<UserCommandResponse>(
            new CreateUserCommand(userId, request.Name, request.Email),
            TimeSpan.FromSeconds(5));
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Event);
    }

    [HttpPut("{userId}/name")]
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

public record CreateUserRequest(string Name, string Email);
public record UpdateNameRequest(string Name);
public record UpdateEmailRequest(string Email);
public record AnswerQuestionnaireRequest(Dictionary<string, string> Answers);
public record ProvideStartValuesRequest(double StartWeight, Dictionary<string, double> Measurements);


