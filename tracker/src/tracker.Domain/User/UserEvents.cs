namespace tracker.Domain.User;

public interface IUserEvent : IWithUserId {}

public sealed record UserRegistered(
    string UserId,
    string Name,
    string Email,
    DateTime RegisteredAt
) : IWithUserId;

public sealed record UserProfileUpdated(
    string UserId,
    string Name,
    string Email,
    DateTime UpdatedAt
) : IWithUserId;

public sealed record UserDeactivated(
    string UserId,
    string Reason,
    DateTime DeactivatedAt
) : IWithUserId;

public sealed record CheckInDataSubmitted(
    string UserId,
    DateTime WeekStart,
    double WeightKg,
    double BodyFatPercent,
    IReadOnlyDictionary<string, double> Measurements,
    DateTime SubmittedAt
) : IWithUserId;