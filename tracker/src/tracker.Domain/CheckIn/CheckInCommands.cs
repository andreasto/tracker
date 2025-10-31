namespace tracker.Domain.CheckIn;

/// <summary>
/// Defines a command that is related to a check-in.
/// </summary>
public interface ICheckInCommand : IWithUserId
{
}

public sealed record SetWeeklyCheckInDayCommand(string UserId, DayOfWeek CheckInDay) : ICheckInCommand;

public sealed record SubmitWeeklyCheckInCommand(
    string UserId,
    double Weight,
    double Thigh,
    double Glutes,
    double Hips,
    double Waist,
    double Stomach,
    double Chest,
    double Overarm) : ICheckInCommand;

public sealed record CheckInCommandResponse(
    string UserId,
    bool IsSuccess,
    ICheckInEvent? Event = null,
    string? ErrorMessage = null) : ICheckInCommand;

public sealed record SubscribeToCheckIn(string UserId, IActorRef Subscriber) : ICheckInCommand;

public sealed record UnsubscribeToCheckIn(string UserId, IActorRef Subscriber) : ICheckInCommand;

