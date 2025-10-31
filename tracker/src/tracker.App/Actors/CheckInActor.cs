using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using tracker.Domain.CheckIn;

namespace tracker.App.Actors;

public record Measurements(
    double Weight,
    double Thigh,
    double Glutes,
    double Hips,
    double Waist,
    double Stomach,
    double Chest,
    double Overarm);

public record WeeklyCheckIn(DateTime SubmittedAt, Measurements Measurements);

public record CheckInState(
    string UserId,
    DayOfWeek? WeeklyCheckInDay = null,
    List<WeeklyCheckIn>? CheckInHistory = null)
{
}

public static class CheckInExtensions
{
    public static CheckInCommandResponse ProcessCommand(this CheckInState state, ICheckInCommand command)
    {
        return command switch
        {
            SetWeeklyCheckInDayCommand setCheckInDay => new CheckInCommandResponse(state.UserId, true,
                new WeeklyCheckInDaySet(setCheckInDay.UserId, setCheckInDay.CheckInDay)),
            SubmitWeeklyCheckInCommand submitCheckIn => new CheckInCommandResponse(state.UserId, true,
                new WeeklyCheckInSubmitted(
                    submitCheckIn.UserId,
                    DateTime.UtcNow,
                    submitCheckIn.Weight,
                    submitCheckIn.Thigh,
                    submitCheckIn.Glutes,
                    submitCheckIn.Hips,
                    submitCheckIn.Waist,
                    submitCheckIn.Stomach,
                    submitCheckIn.Chest,
                    submitCheckIn.Overarm)),
            _ => throw new InvalidOperationException($"Unknown command type: {command.GetType().Name}")
        };
    }

    public static CheckInState ApplyEvent(this CheckInState state, ICheckInEvent @event)
    {
        return @event switch
        {
            WeeklyCheckInDaySet checkInDaySet => state with { WeeklyCheckInDay = checkInDaySet.CheckInDay },
            WeeklyCheckInSubmitted checkInSubmitted => state with
            {
                CheckInHistory = (state.CheckInHistory ?? new List<WeeklyCheckIn>())
                    .Append(new WeeklyCheckIn(
                        checkInSubmitted.SubmittedAt,
                        new Measurements(
                            checkInSubmitted.Weight,
                            checkInSubmitted.Thigh,
                            checkInSubmitted.Glutes,
                            checkInSubmitted.Hips,
                            checkInSubmitted.Waist,
                            checkInSubmitted.Stomach,
                            checkInSubmitted.Chest,
                            checkInSubmitted.Overarm)))
                    .ToList()
            },
            _ => throw new InvalidOperationException($"Unknown event type: {@event.GetType().Name}")
        };
    }
}

public sealed class CheckInActor : ReceivePersistentActor
{
    // currently, do not persist subscribers, but would be easy to add
    private readonly HashSet<IActorRef> _subscribers = new();
    private CheckInState _state;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public CheckInActor(string userId)
    {
        // distinguish both type and entity Id in the EventJournal
        PersistenceId = $"CheckIn_{userId}";
        _state = new CheckInState(userId);

        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is CheckInState s)
            {
                _state = s;
                _log.Info("Recovered check-in state [{0}]", s);
            }
        });

        Recover<ICheckInEvent>(@event => { _state = _state.ApplyEvent(@event); });

        Command<FetchCheckIns>(f => Sender.Tell(_state));

        Command<SubscribeToCheckIn>(subscribe =>
        {
            _subscribers.Add(subscribe.Subscriber);
            Sender.Tell(new CheckInCommandResponse(_state.UserId, true));
            Context.Watch(subscribe.Subscriber);
        });

        Command<UnsubscribeToCheckIn>(unsubscribe =>
        {
            Context.Unwatch(unsubscribe.Subscriber);
            _subscribers.Remove(unsubscribe.Subscriber);
        });

        Command<ICheckInCommand>(cmd =>
        {
            var response = _state.ProcessCommand(cmd);

            if (!response.IsSuccess)
            {
                Sender.Tell(response);
                return;
            }

            if (response.Event != null) // only persist if there is an event to persist
            {
                Persist(response.Event, @event =>
                {
                    _state = _state.ApplyEvent(@event);
                    _log.Info("Updated check-in via {0} - new state is {1}", @event, _state);
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
            SaveSnapshot(_state);
        }
    }

    public override string PersistenceId { get; }
}

