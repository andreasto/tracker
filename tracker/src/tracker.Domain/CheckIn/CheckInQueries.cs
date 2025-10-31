namespace tracker.Domain.CheckIn;

/// <summary>
/// Queries are similar to commands, but they have no side effects.
///
/// They are used to retrieve information from the actors.
/// </summary>
public interface ICheckInQuery : IWithUserId
{
}

public sealed record FetchCheckIns(string UserId) : ICheckInQuery;

