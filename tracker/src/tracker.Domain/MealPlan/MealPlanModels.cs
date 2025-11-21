namespace tracker.Domain.MealPlan;

public record MealPlan
{
    public int MealPlanId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string PlanName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public double TotalCalories { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record MealPlanDay
{
    public int DayId { get; init; }
    public int MealPlanId { get; init; }
    public DateTime PlanDate { get; init; }
    public double DayTotalCalories { get; init; }
}

public record MealPlanMeal
{
    public int MealId { get; init; }
    public int DayId { get; init; }
    public string MealType { get; init; } = string.Empty;
    public double CalorieTarget { get; init; }
}

public record MealTypeDistribution(
    string MealType,
    double Percentage);

public static class MealPlanDefaults
{
    // Default meal distribution percentages
    public static readonly List<MealTypeDistribution> ThreeMealDistribution = new()
    {
        new MealTypeDistribution("breakfast", 0.30),  // 30%
        new MealTypeDistribution("lunch", 0.40),      // 40%
        new MealTypeDistribution("dinner", 0.30)      // 30%
    };

    public static readonly List<MealTypeDistribution> FourMealDistribution = new()
    {
        new MealTypeDistribution("breakfast", 0.25),  // 25%
        new MealTypeDistribution("lunch", 0.35),      // 35%
        new MealTypeDistribution("snack", 0.10),      // 10%
        new MealTypeDistribution("dinner", 0.30)      // 30%
    };

    public static readonly List<MealTypeDistribution> FiveMealDistribution = new()
    {
        new MealTypeDistribution("breakfast", 0.25),  // 25%
        new MealTypeDistribution("snack", 0.10),      // 10% (morning snack)
        new MealTypeDistribution("lunch", 0.30),      // 30%
        new MealTypeDistribution("snack", 0.10),      // 10% (afternoon snack)
        new MealTypeDistribution("dinner", 0.25)      // 25%
    };

    public static List<MealTypeDistribution> GetDistribution(int mealsPerDay)
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

