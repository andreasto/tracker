namespace tracker.Domain.CheckIn;

/// <summary>
/// Events are facts of the system. CheckIn events deal in definitive state changes with check-ins.
/// </summary>
public interface ICheckInEvent : IWithUserId
{
}

public sealed record WeeklyCheckInDaySet(string UserId, DayOfWeek CheckInDay) : ICheckInEvent;

public sealed record WeeklyCheckInSubmitted(
    string UserId,
    DateTime SubmittedAt,
    double Weight,
    double Thigh,
    double Glutes,
    double Hips,
    double Waist,
    double Stomach,
    double Chest,
    double Overarm) : ICheckInEvent;

