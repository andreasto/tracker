namespace tracker.Domain.User;

public interface IUserCommands : IWithUserId
{
}

public sealed record RegisterUser(string UserId, string Name, string Email) : IUserCommands;
public sealed record UpdateUserProfile(string UserId, string Name, string Email) : IUserCommands;
public sealed record DeactivateUser(string UserId, string Reason) : IUserCommands;
public sealed record StartWeeklyCheckIn(string UserId, DateTime WeekStart) : IUserCommands;
public sealed record CompleteWeeklyCheckIn(string UserId, DateTime WeekStart) : IUserCommands;

public sealed record SubmitCheckInData(
    string UserId,
    DateTime WeekStart,
    double WeightKg,
    double BodyFatPercent,
    Dictionary<string, double> Measurements
) : IUserCommands;