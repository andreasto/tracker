using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using tracker.Domain.User;

namespace tracker.App.Actors;

public record User(string UserId, string Name, string Email, bool IsCreated)
{
}

public static class UserExtensions
{
    public static UserCommandResponse ProcessCommand(this User user, IUserCommand command)
    {
        return command switch
        {
            CreateUserCommand create when !user.IsCreated => new UserCommandResponse(user.UserId, true,
                new UserCreated(create.UserId, create.Name, create.Email)),
            CreateUserCommand create when user.IsCreated => new UserCommandResponse(user.UserId, false,
                ErrorMessage: "User already exists"),
            UpdateUserNameCommand updateName when user.IsCreated => new UserCommandResponse(user.UserId, true,
                new UserNameUpdated(updateName.UserId, updateName.Name)),
            UpdateUserNameCommand updateName when !user.IsCreated => new UserCommandResponse(user.UserId, false,
                ErrorMessage: "User does not exist"),
            UpdateUserEmailCommand updateEmail when user.IsCreated => new UserCommandResponse(user.UserId, true,
                new UserEmailUpdated(updateEmail.UserId, updateEmail.Email)),
            UpdateUserEmailCommand updateEmail when !user.IsCreated => new UserCommandResponse(user.UserId, false,
                ErrorMessage: "User does not exist"),
            _ => throw new InvalidOperationException($"Unknown command type: {command.GetType().Name}")
        };
    }

    public static User ApplyEvent(this User user, IUserEvent @event)
    {
        return @event switch
        {
            UserCreated created => user with { Name = created.Name, Email = created.Email, IsCreated = true },
            UserNameUpdated nameUpdated => user with { Name = nameUpdated.Name },
            UserEmailUpdated emailUpdated => user with { Email = emailUpdated.Email },
            _ => throw new InvalidOperationException($"Unknown event type: {@event.GetType().Name}")
        };
    }
}

public sealed class UserActor : ReceivePersistentActor
{
    // currently, do not persist subscribers, but would be easy to add
    private readonly HashSet<IActorRef> _subscribers = new();
    private User _user;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public UserActor(string userId)
    {
        // distinguish both type and entity Id in the EventJournal
        PersistenceId = $"User_{userId}";
        _user = new User(userId, string.Empty, string.Empty, false);

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is User u)
            {
                _user = u;
                _log.Info("Recovered user [{0}]", u);
            }
        });

        Recover<IUserEvent>(@event => { _user = _user.ApplyEvent(@event); });

        Command<FetchUser>(f => Sender.Tell(_user));

        Command<SubscribeToUser>(subscribe =>
        {
            _subscribers.Add(subscribe.Subscriber);
            Sender.Tell(new UserCommandResponse(_user.UserId, true));
            Context.Watch(subscribe.Subscriber);
        });

        Command<UnsubscribeToUser>(unsubscribe =>
        {
            Context.Unwatch(unsubscribe.Subscriber);
            _subscribers.Remove(unsubscribe.Subscriber);
        });

        Command<IUserCommand>(cmd =>
        {
            var response = _user.ProcessCommand(cmd);

            if (!response.IsSuccess)
            {
                Sender.Tell(response);
                return;
            }

            if (response.Event != null) // only persist if there is an event to persist
            {
                Persist(response.Event, @event =>
                {
                    _user = _user.ApplyEvent(@event);
                    _log.Info("Updated user via {0} - new state is {1}", @event, _user);
                    Sender.Tell(response);

                    // push events to all subscribers
                    foreach (var s in _subscribers)
                    {
                        s.Tell(@event);
                    }

                    SaveSnapshotWhenAble();
                });
            }
        });

        Command<SaveSnapshotSuccess>(success =>
        {
            // delete all older snapshots (but leave journal intact, in case we want to do projections with that data)
            DeleteSnapshots(new SnapshotSelectionCriteria(success.Metadata.SequenceNr - 1));
        });
    }

    private void SaveSnapshotWhenAble()
    {
        // save a new snapshot every 25 events, in order to keep recovery times bounded
        if (LastSequenceNr % 25 == 0)
        {
            SaveSnapshot(_user);
        }
    }

    public override string PersistenceId { get; }
}

