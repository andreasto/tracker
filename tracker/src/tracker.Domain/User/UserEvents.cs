namespace tracker.Domain.User;

/// <summary>
/// Events are facts of the system. User events deal in definitive state changes with the user.
/// </summary>
public interface IUserEvent : IWithUserId
{
}

public sealed record UserCreated(string UserId, string Name, string Email) : IUserEvent;

public sealed record UserNameUpdated(string UserId, string Name) : IUserEvent;

public sealed record UserEmailUpdated(string UserId, string Email) : IUserEvent;

