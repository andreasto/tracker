using Akka.TestKit.Xunit2;
using tracker.App.Actors;
using tracker.Domain.CheckIn;

namespace tracker.App.Tests;

public class CheckInActorSpecs : TestKit
{
    [Fact]
    public async Task CheckInActor_should_set_weekly_checkin_day()
    {
        // Arrange
        var checkInActor = Sys.ActorOf(Props.Create(() => new CheckInActor("user1")));
        
        // Act
        var result = await checkInActor.Ask<CheckInCommandResponse>(
            new SetWeeklyCheckInDayCommand("user1", DayOfWeek.Monday), 
            TimeSpan.FromSeconds(3));
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Event.Should().BeOfType<WeeklyCheckInDaySet>();
        var @event = (WeeklyCheckInDaySet)result.Event!;
        @event.CheckInDay.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public async Task CheckInActor_should_submit_weekly_checkin()
    {
        // Arrange
        var checkInActor = Sys.ActorOf(Props.Create(() => new CheckInActor("user1")));
        
        // Act
        var result = await checkInActor.Ask<CheckInCommandResponse>(
            new SubmitWeeklyCheckInCommand(
                "user1",
                Weight: 75.5,
                Thigh: 60.0,
                Glutes: 95.0,
                Hips: 100.0,
                Waist: 80.0,
                Stomach: 85.0,
                Chest: 95.0,
                Overarm: 35.0), 
            TimeSpan.FromSeconds(3));
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Event.Should().BeOfType<WeeklyCheckInSubmitted>();
        var @event = (WeeklyCheckInSubmitted)result.Event!;
        @event.Weight.Should().Be(75.5);
        @event.Thigh.Should().Be(60.0);
        @event.Glutes.Should().Be(95.0);
    }

    [Fact]
    public async Task CheckInActor_should_store_checkin_history()
    {
        // Arrange
        var checkInActor = Sys.ActorOf(Props.Create(() => new CheckInActor("user1")));
        
        // Act - Submit first check-in
        await checkInActor.Ask<CheckInCommandResponse>(
            new SubmitWeeklyCheckInCommand(
                "user1",
                Weight: 75.5,
                Thigh: 60.0,
                Glutes: 95.0,
                Hips: 100.0,
                Waist: 80.0,
                Stomach: 85.0,
                Chest: 95.0,
                Overarm: 35.0), 
            TimeSpan.FromSeconds(3));
        
        // Act - Submit second check-in
        await checkInActor.Ask<CheckInCommandResponse>(
            new SubmitWeeklyCheckInCommand(
                "user1",
                Weight: 74.5,
                Thigh: 59.0,
                Glutes: 94.0,
                Hips: 99.0,
                Waist: 79.0,
                Stomach: 84.0,
                Chest: 94.0,
                Overarm: 34.5), 
            TimeSpan.FromSeconds(3));
        
        // Act - Fetch state
        var state = await checkInActor.Ask<CheckInState>(
            new FetchCheckIns("user1"), 
            TimeSpan.FromSeconds(3));
        
        // Assert
        state.CheckInHistory.Should().NotBeNull();
        state.CheckInHistory!.Count.Should().Be(2);
        state.CheckInHistory[0].Measurements.Weight.Should().Be(75.5);
        state.CheckInHistory[1].Measurements.Weight.Should().Be(74.5);
    }
}

