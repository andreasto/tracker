using Akka.Hosting;
using Akka.Hosting.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using tracker.App.Actors;
using tracker.App.Configuration;
using tracker.Domain.User;
using Xunit.Abstractions;

namespace tracker.App.Tests;

public class UserActorOnboardingSpecs : TestKit
{
    public UserActorOnboardingSpecs(ITestOutputHelper output) : base(output: output)
    {
    }

    [Fact]
    public void UserActor_should_follow_onboarding_flow()
    {
        // arrange
        var userActor = ActorRegistry.Get<UserActor>();
        var userId = "test-user-123";
        
        // act & assert - Step 1: Create User
        userActor.Tell(new CreateUserCommand(userId, "John Doe", "john@example.com"), TestActor);
        var createResponse = ExpectMsg<UserCommandResponse>();
        createResponse.IsSuccess.Should().BeTrue();
        createResponse.Event.Should().BeOfType<UserCreated>();
        
        // Verify user is in Registered state
        userActor.Tell(new FetchUser(userId), TestActor);
        var user1 = ExpectMsg<User>();
        user1.OnboardingState.Should().Be(UserOnboardingState.Registered);
        user1.Name.Should().Be("John Doe");
        user1.Email.Should().Be("john@example.com");

        // act & assert - Step 2: Try to skip questionnaire (should fail)
        userActor.Tell(new ProvideStartValuesCommand(userId, 75.5, new Dictionary<string, double>
        {
            { "chest", 95.0 },
            { "waist", 85.0 }
        }), TestActor);
        var skipResponse = ExpectMsg<UserCommandResponse>();
        skipResponse.IsSuccess.Should().BeFalse();
        skipResponse.ErrorMessage.Should().Contain("questionnaire");

        // act & assert - Step 3: Answer Questionnaire
        userActor.Tell(new AnswerQuestionnaireCommand(userId, new Dictionary<string, string>
        {
            { "activityLevel", "moderate" },
            { "goals", "weight-loss" },
            { "dietaryRestrictions", "none" }
        }), TestActor);
        var questionnaireResponse = ExpectMsg<UserCommandResponse>();
        questionnaireResponse.IsSuccess.Should().BeTrue();
        questionnaireResponse.Event.Should().BeOfType<QuestionnaireAnswered>();
        
        // Verify state transition
        userActor.Tell(new FetchUser(userId), TestActor);
        var user2 = ExpectMsg<User>();
        user2.OnboardingState.Should().Be(UserOnboardingState.QuestionnaireAnswered);
        user2.QuestionnaireAnswers.Should().NotBeNull();
        user2.QuestionnaireAnswers!["activityLevel"].Should().Be("moderate");

        // act & assert - Step 4: Provide Start Values
        userActor.Tell(new ProvideStartValuesCommand(userId, 75.5, new Dictionary<string, double>
        {
            { "chest", 95.0 },
            { "waist", 85.0 },
            { "hips", 100.0 }
        }), TestActor);
        var startValuesResponse = ExpectMsg<UserCommandResponse>();
        startValuesResponse.IsSuccess.Should().BeTrue();
        startValuesResponse.Event.Should().BeOfType<StartValuesProvided>();
        
        // Verify state transition
        userActor.Tell(new FetchUser(userId), TestActor);
        var user3 = ExpectMsg<User>();
        user3.OnboardingState.Should().Be(UserOnboardingState.StartValuesProvided);
        user3.StartWeight.Should().Be(75.5);
        user3.StartMeasurements.Should().NotBeNull();
        user3.StartMeasurements!["chest"].Should().Be(95.0);

        // act & assert - Step 5: Complete Onboarding
        userActor.Tell(new CompleteOnboardingCommand(userId), TestActor);
        var completeResponse = ExpectMsg<UserCommandResponse>();
        completeResponse.IsSuccess.Should().BeTrue();
        completeResponse.Event.Should().BeOfType<UserOnboardingCompleted>();
        
        // Verify final state
        userActor.Tell(new FetchUser(userId), TestActor);
        var user4 = ExpectMsg<User>();
        user4.OnboardingState.Should().Be(UserOnboardingState.Complete);
    }

    [Fact]
    public void UserActor_should_reject_questionnaire_after_completion()
    {
        // arrange
        var userActor = ActorRegistry.Get<UserActor>();
        var userId = "test-user-456";
        
        // Complete full onboarding flow
        userActor.Tell(new CreateUserCommand(userId, "Jane Doe", "jane@example.com"), TestActor);
        ExpectMsg<UserCommandResponse>();
        
        userActor.Tell(new AnswerQuestionnaireCommand(userId, new Dictionary<string, string>
        {
            { "activityLevel", "high" }
        }), TestActor);
        ExpectMsg<UserCommandResponse>();
        
        userActor.Tell(new ProvideStartValuesCommand(userId, 65.0, new Dictionary<string, double>
        {
            { "waist", 70.0 }
        }), TestActor);
        ExpectMsg<UserCommandResponse>();
        
        userActor.Tell(new CompleteOnboardingCommand(userId), TestActor);
        ExpectMsg<UserCommandResponse>();

        // act - Try to answer questionnaire again
        userActor.Tell(new AnswerQuestionnaireCommand(userId, new Dictionary<string, string>
        {
            { "activityLevel", "low" }
        }), TestActor);
        
        // assert
        var response = ExpectMsg<UserCommandResponse>();
        response.IsSuccess.Should().BeFalse();
        response.ErrorMessage.Should().Contain("questionnaire");
    }

    [Fact]
    public void UserActor_should_allow_name_updates_during_onboarding()
    {
        // arrange
        var userActor = ActorRegistry.Get<UserActor>();
        var userId = "test-user-789";
        
        // Create user
        userActor.Tell(new CreateUserCommand(userId, "Original Name", "user@example.com"), TestActor);
        ExpectMsg<UserCommandResponse>();
        
        // act - Update name while in Registered state
        userActor.Tell(new UpdateUserNameCommand(userId, "Updated Name"), TestActor);
        var updateResponse = ExpectMsg<UserCommandResponse>();
        
        // assert
        updateResponse.IsSuccess.Should().BeTrue();
        
        userActor.Tell(new FetchUser(userId), TestActor);
        var user = ExpectMsg<User>();
        user.Name.Should().Be("Updated Name");
        user.OnboardingState.Should().Be(UserOnboardingState.Registered); // State should not change
    }

    protected override void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        var settings = new AkkaSettings() { UseClustering = false, PersistenceMode = PersistenceMode.InMemory };
        services.AddSingleton(settings);
        base.ConfigureServices(context, services);
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.ConfigureUserActors(provider).ConfigurePersistence(provider);
    }
}

