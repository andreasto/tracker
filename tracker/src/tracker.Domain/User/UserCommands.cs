namespace tracker.Domain.User;

/// <summary>
/// Defines a command that is related to a user.
/// </summary>
public interface IUserCommand : IWithUserId
{
}

public record SetPasswordCommand(string UserId, string Password) : IUserCommand;

public record AuthenticateCommand(string UserId, string Password) : IUserCommand;

public record PasswordSetEvent(string UserId, string HashedPassword, DateTime SetAt) : IUserEvent;

public record AuthenticationAttemptedEvent(string UserId, bool Success, DateTime AttemptedAt) : IUserEvent;

public sealed record CreateUserCommand(string UserId, string Name, string Email) : IUserCommand;

public sealed record UpdateUserNameCommand(string UserId, string Name) : IUserCommand;

public sealed record UpdateUserEmailCommand(string UserId, string Email) : IUserCommand;

public sealed record AnswerQuestionnaireCommand(string UserId, Dictionary<string, string> Answers) : IUserCommand;

public sealed record ProvideStartValuesCommand(
    string UserId, 
    double StartWeight, 
    Dictionary<string, double> Measurements) : IUserCommand;

public sealed record CompleteOnboardingCommand(string UserId) : IUserCommand;

public sealed record UserCommandResponse(
    string UserId,
    bool IsSuccess,
    IUserEvent? Event = null,
    string? ErrorMessage = null) : IUserCommand;

public sealed record SubscribeToUser(string UserId, IActorRef Subscriber) : IUserCommand;

public sealed record UnsubscribeToUser(string UserId, IActorRef Subscriber) : IUserCommand;


