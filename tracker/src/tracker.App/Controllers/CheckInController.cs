using Akka.Actor;
using Akka.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tracker.App.Actors;
using tracker.Domain.CheckIn;

namespace tracker.App.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class CheckInController : ControllerBase
{
    private readonly ILogger<CheckInController> _logger;
    private readonly IActorRef _checkInActor;

    public CheckInController(ILogger<CheckInController> logger, IRequiredActor<CheckInActor> checkInActor)
    {
        _logger = logger;
        _checkInActor = checkInActor.ActorRef;
    }

    [HttpGet("{userId}")]
    public async Task<CheckInState> Get(string userId)
    {
        var checkIns = await _checkInActor.Ask<CheckInState>(new FetchCheckIns(userId), TimeSpan.FromSeconds(5));
        return checkIns;
    }

    [HttpPut("{userId}/checkin-day")]
    public async Task<IActionResult> SetWeeklyCheckInDay(string userId, [FromBody] SetWeeklyCheckInDayRequest request)
    {
        var result = await _checkInActor.Ask<CheckInCommandResponse>(
            new SetWeeklyCheckInDayCommand(userId, request.CheckInDay),
            TimeSpan.FromSeconds(5));
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Event);
    }

    [HttpPost("{userId}")]
    public async Task<IActionResult> SubmitWeeklyCheckIn(string userId, [FromBody] SubmitWeeklyCheckInRequest request)
    {
        var result = await _checkInActor.Ask<CheckInCommandResponse>(
            new SubmitWeeklyCheckInCommand(
                userId,
                request.Weight,
                request.Thigh,
                request.Glutes,
                request.Hips,
                request.Waist,
                request.Stomach,
                request.Chest,
                request.Overarm),
            TimeSpan.FromSeconds(5));
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(result.Event);
    }
}

public record SetWeeklyCheckInDayRequest(DayOfWeek CheckInDay);
public record SubmitWeeklyCheckInRequest(
    double Weight,
    double Thigh,
    double Glutes,
    double Hips,
    double Waist,
    double Stomach,
    double Chest,
    double Overarm);

