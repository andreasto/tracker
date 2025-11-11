using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using tracker.Domain.User;

namespace tracker.App.Actors;

public record User(
    string UserId, 
    string Name, 
    string Email, 
    bool IsCreated,
    UserOnboardingState OnboardingState = UserOnboardingState.NotStarted,
    Dictionary<string, string>? QuestionnaireAnswers = null,
    double? StartWeight = null,
    Dictionary<string, double>? StartMeasurements = null,
    double? BmrBase = null,
    double? BmrWithActivityLevel = null)
{
}

public enum UserOnboardingState
{
    NotStarted,
    Registered,
    QuestionnaireAnswered,
    StartValuesProvided,
    Complete
}

public static class BmrCalculator
{
    public static (double bmrBase, double bmrWithActivityLevel) Calculate(
        User user)
    {
        if (user.QuestionnaireAnswers == null || user.StartWeight == null)
        {
            throw new InvalidOperationException("Cannot calculate BMR without questionnaire answers and start weight");
        }

        // Extract required data from questionnaire
        if (!user.QuestionnaireAnswers.TryGetValue("gender", out var gender))
            throw new InvalidOperationException("Gender is required to calculate BMR");
        
        if (!user.QuestionnaireAnswers.TryGetValue("age", out var ageStr) || !int.TryParse(ageStr, out var age))
            throw new InvalidOperationException("Valid age is required to calculate BMR");
        
        if (!user.QuestionnaireAnswers.TryGetValue("height", out var heightStr) || !double.TryParse(heightStr, out var height))
            throw new InvalidOperationException("Valid height (in cm) is required to calculate BMR");
        
        if (!user.QuestionnaireAnswers.TryGetValue("activityLevel", out var activityLevel))
            throw new InvalidOperationException("Activity level is required to calculate BMR");

        var weight = user.StartWeight.Value;

        // Calculate base BMR using Mifflin-St Jeor Equation
        double bmrBase;
        if (gender.ToLowerInvariant() == "male" || gender.ToLowerInvariant() == "m")
        {
            // For men: BMR = 66.5 + (13.75 × weight in kg) + (5.003 × height in cm) - (6.75 × age)
            bmrBase = 66.5 + (13.75 * weight) + (5.003 * height) - (6.75 * age);
        }
        else if (gender.ToLowerInvariant() == "female" || gender.ToLowerInvariant() == "f")
        {
            // For women: BMR = 655.1 + (9.563 × weight in kg) + (1.850 × height in cm) - (4.676 × age)
            bmrBase = 655.1 + (9.563 * weight) + (1.850 * height) - (4.676 * age);
        }
        else
        {
            throw new InvalidOperationException($"Invalid gender value: {gender}. Must be 'male', 'm', 'female', or 'f'");
        }

        // Calculate BMR with activity level multiplier
        double activityMultiplier = activityLevel.ToLowerInvariant() switch
        {
            "sedentary" => 1.2,
            "lightly active" or "lightlyactive" or "lightly_active" => 1.375,
            "moderately active" or "moderatelyactive" or "moderately_active" or "moderate" => 1.55,
            "very active" or "veryactive" or "very_active" => 1.725,
            "extra active" or "extraactive" or "extra_active" => 1.9,
            _ => throw new InvalidOperationException($"Invalid activity level: {activityLevel}")
        };

        double bmrWithActivityLevel = bmrBase * activityMultiplier;

        return (bmrBase, bmrWithActivityLevel);
    }
}

public static class UserExtensions
{
    private static UserCommandResponse CompleteOnboardingWithBmr(User user, CompleteOnboardingCommand complete)
    {
        try
        {
            var (bmrBase, bmrWithActivityLevel) = BmrCalculator.Calculate(user);
            return new UserCommandResponse(user.UserId, true, 
                new UserOnboardingCompleted(complete.UserId, bmrBase, bmrWithActivityLevel));
        }
        catch (InvalidOperationException ex)
        {
            return new UserCommandResponse(user.UserId, false, ErrorMessage: ex.Message);
        }
    }

    public static UserCommandResponse ProcessCommand(this User user, IUserCommand command)
    {
        return command switch
        {
            CreateUserCommand create when !user.IsCreated => new UserCommandResponse(user.UserId, true,
                new UserCreated(create.UserId, create.Name, create.Email)),
            CreateUserCommand _ when user.IsCreated => new UserCommandResponse(user.UserId, false,
                ErrorMessage: "User already exists"),
            UpdateUserNameCommand updateName when user.IsCreated => new UserCommandResponse(user.UserId, true,
                new UserNameUpdated(updateName.UserId, updateName.Name)),
            UpdateUserNameCommand _ when !user.IsCreated => new UserCommandResponse(user.UserId, false,
                ErrorMessage: "User does not exist"),
            UpdateUserEmailCommand updateEmail when user.IsCreated => new UserCommandResponse(user.UserId, true,
                new UserEmailUpdated(updateEmail.UserId, updateEmail.Email)),
            UpdateUserEmailCommand _ when !user.IsCreated => new UserCommandResponse(user.UserId, false,
                ErrorMessage: "User does not exist"),
            
            // Onboarding flow commands
            AnswerQuestionnaireCommand answerQuestionnaire when user.OnboardingState == UserOnboardingState.Registered =>
                new UserCommandResponse(user.UserId, true, new QuestionnaireAnswered(answerQuestionnaire.UserId, answerQuestionnaire.Answers)),
            AnswerQuestionnaireCommand _ when user.OnboardingState != UserOnboardingState.Registered =>
                new UserCommandResponse(user.UserId, false, ErrorMessage: $"Cannot answer questionnaire in state: {user.OnboardingState}"),
            
            ProvideStartValuesCommand provideValues when user.OnboardingState == UserOnboardingState.QuestionnaireAnswered =>
                new UserCommandResponse(user.UserId, true, 
                    new StartValuesProvided(provideValues.UserId, provideValues.StartWeight, provideValues.Measurements)),
            ProvideStartValuesCommand _ when user.OnboardingState != UserOnboardingState.QuestionnaireAnswered =>
                new UserCommandResponse(user.UserId, false, ErrorMessage: $"Cannot provide start values in state: {user.OnboardingState}"),
            
            CompleteOnboardingCommand complete when user.OnboardingState == UserOnboardingState.StartValuesProvided =>
                CompleteOnboardingWithBmr(user, complete),
            CompleteOnboardingCommand _ when user.OnboardingState != UserOnboardingState.StartValuesProvided =>
                new UserCommandResponse(user.UserId, false, ErrorMessage: $"Cannot complete onboarding in state: {user.OnboardingState}"),
            
            _ => throw new InvalidOperationException($"Unknown command type: {command.GetType().Name}")
        };
    }

    public static User ApplyEvent(this User user, IUserEvent @event)
    {
        return @event switch
        {
            UserCreated created => user with 
            { 
                Name = created.Name, 
                Email = created.Email, 
                IsCreated = true,
                OnboardingState = UserOnboardingState.Registered
            },
            UserNameUpdated nameUpdated => user with { Name = nameUpdated.Name },
            UserEmailUpdated emailUpdated => user with { Email = emailUpdated.Email },
            QuestionnaireAnswered answered => user with 
            { 
                QuestionnaireAnswers = answered.Answers,
                OnboardingState = UserOnboardingState.QuestionnaireAnswered
            },
            StartValuesProvided provided => user with 
            { 
                StartWeight = provided.StartWeight,
                StartMeasurements = provided.Measurements,
                OnboardingState = UserOnboardingState.StartValuesProvided
            },
            UserOnboardingCompleted completed => user with 
            { 
                OnboardingState = UserOnboardingState.Complete,
                BmrBase = completed.BmrBase,
                BmrWithActivityLevel = completed.BmrWithActivityLevel
            },
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

        // Set up command handlers based on recovered state
        Command<IUserCommand>(_ => { });  // Placeholder, will be replaced by SetupBehavior
        Command<FetchUser>(_ => { });  // Placeholder
        
        // Common commands that work in all states
        Command<SaveSnapshotSuccess>(success =>
        {
            // delete all older snapshots (but leave journal intact, in case we want to do projections with that data)
            DeleteSnapshots(new SnapshotSelectionCriteria(success.Metadata.SequenceNr - 1));
        });
        
        // After recovery completes, set up the proper behavior based on current state
        Recover<RecoveryCompleted>(_ => { SetupBehavior(_user.OnboardingState); });
    }

    private void SetupBehavior(UserOnboardingState state)
    {
        _log.Info("Setting up behavior for state: {0}", state);
        
        switch (state)
        {
            case UserOnboardingState.NotStarted:
                Become(NotStartedBehavior);
                break;
            case UserOnboardingState.Registered:
                Become(RegisteredBehavior);
                break;
            case UserOnboardingState.QuestionnaireAnswered:
                Become(QuestionnaireAnsweredBehavior);
                break;
            case UserOnboardingState.StartValuesProvided:
                Become(StartValuesProvidedBehavior);
                break;
            case UserOnboardingState.Complete:
                Become(CompleteBehavior);
                break;
        }
    }

    private void NotStartedBehavior()
    {
        SetupCommonHandlers();
        
        Command<CreateUserCommand>(cmd =>
        {
            var response = _user.ProcessCommand(cmd);
            HandleCommandResponse(response, () => Become(RegisteredBehavior));
        });
        
        Command<IUserCommand>(cmd =>
        {
            Sender.Tell(new UserCommandResponse(_user.UserId, false, 
                ErrorMessage: "User must be created first"));
        });
    }

    private void RegisteredBehavior()
    {
        SetupCommonHandlers();
        _log.Info("User {0} is in Registered state, waiting for questionnaire", _user.UserId);
        
        Command<AnswerQuestionnaireCommand>(cmd =>
        {
            var response = _user.ProcessCommand(cmd);
            HandleCommandResponse(response, () => Become(QuestionnaireAnsweredBehavior));
        });
        
        Command<UpdateUserNameCommand>(HandleStandardCommand);
        Command<UpdateUserEmailCommand>(HandleStandardCommand);
        
        Command<IUserCommand>(cmd =>
        {
            Sender.Tell(new UserCommandResponse(_user.UserId, false, 
                ErrorMessage: "Please complete the questionnaire first"));
        });
    }

    private void QuestionnaireAnsweredBehavior()
    {
        SetupCommonHandlers();
        _log.Info("User {0} has answered questionnaire, waiting for start values", _user.UserId);
        
        Command<ProvideStartValuesCommand>(cmd =>
        {
            var response = _user.ProcessCommand(cmd);
            HandleCommandResponse(response, () => Become(StartValuesProvidedBehavior));
        });
        
        Command<UpdateUserNameCommand>(HandleStandardCommand);
        Command<UpdateUserEmailCommand>(HandleStandardCommand);
        
        Command<IUserCommand>(cmd =>
        {
            Sender.Tell(new UserCommandResponse(_user.UserId, false, 
                ErrorMessage: "Please provide start measurements and weight"));
        });
    }

    private void StartValuesProvidedBehavior()
    {
        SetupCommonHandlers();
        _log.Info("User {0} has provided start values, waiting for completion", _user.UserId);
        
        Command<CompleteOnboardingCommand>(cmd =>
        {
            var response = _user.ProcessCommand(cmd);
            HandleCommandResponse(response, () => Become(CompleteBehavior));
        });
        
        Command<UpdateUserNameCommand>(HandleStandardCommand);
        Command<UpdateUserEmailCommand>(HandleStandardCommand);
        
        Command<IUserCommand>(cmd =>
        {
            Sender.Tell(new UserCommandResponse(_user.UserId, false, 
                ErrorMessage: "Please complete onboarding"));
        });
    }

    private void CompleteBehavior()
    {
        SetupCommonHandlers();
        _log.Info("User {0} has completed onboarding", _user.UserId);
        
        Command<UpdateUserNameCommand>(HandleStandardCommand);
        Command<UpdateUserEmailCommand>(HandleStandardCommand);
        
        Command<IUserCommand>(cmd =>
        {
            var response = _user.ProcessCommand(cmd);
            HandleCommandResponse(response);
        });
    }

    private void SetupCommonHandlers()
    {
        Command<FetchUser>(_ => Sender.Tell(_user));

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

        Command<SaveSnapshotSuccess>(success =>
        {
            DeleteSnapshots(new SnapshotSelectionCriteria(success.Metadata.SequenceNr - 1));
        });
    }

    private void HandleStandardCommand(IUserCommand cmd)
    {
        var response = _user.ProcessCommand(cmd);
        HandleCommandResponse(response);
    }

    private void HandleCommandResponse(UserCommandResponse response, Action? onSuccess = null)
    {
        if (!response.IsSuccess)
        {
            Sender.Tell(response);
            return;
        }

        if (response.Event != null)
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
                
                // Transition to new state if needed
                onSuccess?.Invoke();
            });
        }
        else
        {
            Sender.Tell(response);
        }
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

