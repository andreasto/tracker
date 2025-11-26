using tracker.Domain.User;

namespace tracker.Domain.MealPlan;

public interface IMealPlanCommand : IWithUserId
{
}

public sealed record CreateMealPlanCommand(
    string UserId,
    string PlanName,
    int BreakfastCount,
    int LunchCount,
    int DinnerCount,
    int SnackCount) : IMealPlanCommand;

public sealed record MealPlanCommandResponse(
    string UserId,
    bool IsSuccess,
    int? MealPlanId = null,
    string? ErrorMessage = null) : IMealPlanCommand;

