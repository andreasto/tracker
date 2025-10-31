namespace tracker.Domain.CheckIn;

/// <summary>
/// All messages decorated with this interface belong to a specific user's check-in data.
/// </summary>
public interface IWithUserId
{
    string UserId { get; }
}