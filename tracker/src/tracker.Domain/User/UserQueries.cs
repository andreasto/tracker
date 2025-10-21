namespace tracker.Domain.User;

public interface IUserQuery : IWithUserId
{
    
}

public sealed record GetUserProfile(string UserId) : IUserQuery;
public sealed record GetUserStatus(string UserId) : IUserQuery;