namespace tracker.Domain.MealPlan;

/// <summary>
/// Marker interface for all meal plan-related commands and events.
/// </summary>
public interface IWithMealPlanId
{
    int MealPlanId { get; }
}

