namespace tracker.Domain.MealPlan;

public record MealPlan(
    int MealPlanId,
    string UserId,
    string PlanName,
    DateTime StartDate,
    DateTime? EndDate,
    double TotalCalories,
    DateTime CreatedAt);

public record MealPlanDay(
    int DayId,
    int MealPlanId,
    DateTime PlanDate,
    double DayTotalCalories);

public record MealPlanMeal(
    int MealId,
    int DayId,
    string MealType,
    double CalorieTarget);

public record MealTypeDistribution(
    string MealType,
    double Percentage);

public static class MealPlanDefaults
{
    // Default meal distribution percentages
    public static readonly Dictionary<string, double> ThreeMealDistribution = new()
    {
        { "breakfast", 0.30 },  // 30%
        { "lunch", 0.40 },      // 40%
        { "dinner", 0.30 }      // 30%
    };

    public static readonly Dictionary<string, double> FourMealDistribution = new()
    {
        { "breakfast", 0.25 },  // 25%
        { "lunch", 0.35 },      // 35%
        { "snack", 0.10 },      // 10%
        { "dinner", 0.30 }      // 30%
    };

    public static readonly Dictionary<string, double> FiveMealDistribution = new()
    {
        { "breakfast", 0.25 },  // 25%
        { "snack", 0.10 },      // 10% (morning snack)
        { "lunch", 0.30 },      // 30%
        { "snack", 0.10 },      // 10% (afternoon snack)
        { "dinner", 0.25 }      // 25%
    };

    public static Dictionary<string, double> GetDistribution(int mealsPerDay)
    {
        return mealsPerDay switch
        {
            3 => ThreeMealDistribution,
            4 => FourMealDistribution,
            5 => FiveMealDistribution,
            _ => ThreeMealDistribution // Default to 3 meals if unknown
        };
    }
}

