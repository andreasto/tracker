using Npgsql;
using Dapper;
using tracker.Domain.MealPlan;

namespace tracker.App.Services;

public interface IMealPlanService
{
    Task<MealPlanCommandResponse> CreateMealPlanAsync(
        string userId,
        string planName,
        DateTime startDate,
        DateTime? endDate,
        double bmrWithActivityLevel,
        int mealsPerDay);
    
    Task<MealPlan?> GetMealPlanAsync(int mealPlanId);
    Task<List<MealPlan>> GetUserMealPlansAsync(string userId);
    Task<MealPlanDetails?> GetMealPlanDetailsAsync(int mealPlanId);
}

public record MealPlanDetails(
    MealPlan Plan,
    List<DayWithMeals> Days);

public record DayWithMeals(
    MealPlanDay Day,
    List<MealPlanMeal> Meals);

public class MealPlanService : IMealPlanService
{
    private readonly string _connectionString;

    public MealPlanService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<MealPlanCommandResponse> CreateMealPlanAsync(
        string userId,
        string planName,
        DateTime startDate,
        DateTime? endDate,
        double bmrWithActivityLevel,
        int mealsPerDay)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Insert meal plan
                var mealPlanId = await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO mealplans (user_id, plan_name, start_date, end_date, total_calories)
                      VALUES (@UserId, @PlanName, @StartDate, @EndDate, @TotalCalories)
                      RETURNING mealplan_id",
                    new
                    {
                        UserId = userId,
                        PlanName = planName,
                        StartDate = startDate.Date,
                        EndDate = endDate?.Date,
                        TotalCalories = bmrWithActivityLevel
                    },
                    transaction);

                // Get meal distribution based on meals per day
                var distribution = MealPlanDefaults.GetDistribution(mealsPerDay);

                // Calculate number of days
                var daysToCreate = endDate.HasValue 
                    ? (endDate.Value.Date - startDate.Date).Days + 1 
                    : 7; // Default to 1 week if no end date

                // Create days and meals
                for (int i = 0; i < daysToCreate; i++)
                {
                    var planDate = startDate.Date.AddDays(i);

                    // Insert day
                    var dayId = await connection.ExecuteScalarAsync<int>(
                        @"INSERT INTO mealplan_days (mealplan_id, plan_date, day_total_calories)
                          VALUES (@MealPlanId, @PlanDate, @DayTotalCalories)
                          RETURNING day_id",
                        new
                        {
                            MealPlanId = mealPlanId,
                            PlanDate = planDate,
                            DayTotalCalories = bmrWithActivityLevel
                        },
                        transaction);

                    // Insert meals for the day
                    foreach (var meal in distribution)
                    {
                        var calorieTarget = bmrWithActivityLevel * meal.Percentage;

                        await connection.ExecuteAsync(
                            @"INSERT INTO mealplan_meals (day_id, meal_type, calorie_target)
                              VALUES (@DayId, @MealType, @CalorieTarget)",
                            new
                            {
                                DayId = dayId,
                                MealType = meal.MealType,
                                CalorieTarget = calorieTarget
                            },
                            transaction);
                    }
                }

                await transaction.CommitAsync();

                return new MealPlanCommandResponse(
                    UserId: userId,
                    IsSuccess: true,
                    MealPlanId: mealPlanId);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            return new MealPlanCommandResponse(
                UserId: userId,
                IsSuccess: false,
                ErrorMessage: $"Failed to create meal plan: {ex.Message}");
        }
    }

    public async Task<MealPlan?> GetMealPlanAsync(int mealPlanId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        return await connection.QuerySingleOrDefaultAsync<MealPlan>(
            @"SELECT mealplan_id as MealPlanId, user_id as UserId, plan_name as PlanName, 
                     start_date as StartDate, end_date as EndDate, total_calories as TotalCalories,
                     created_at as CreatedAt
              FROM mealplans
              WHERE mealplan_id = @MealPlanId",
            new { MealPlanId = mealPlanId });
    }

    public async Task<List<MealPlan>> GetUserMealPlansAsync(string userId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var result = await connection.QueryAsync<MealPlan>(
            @"SELECT mealplan_id as MealPlanId, user_id as UserId, plan_name as PlanName, 
                     start_date as StartDate, end_date as EndDate, total_calories as TotalCalories,
                     created_at as CreatedAt
              FROM mealplans
              WHERE user_id = @UserId
              ORDER BY created_at DESC",
            new { UserId = userId });

        return result.ToList();
    }

    public async Task<MealPlanDetails?> GetMealPlanDetailsAsync(int mealPlanId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Get the meal plan
        var plan = await GetMealPlanAsync(mealPlanId);
        if (plan == null) return null;

        // Get all days for this plan
        var days = await connection.QueryAsync<MealPlanDay>(
            @"SELECT day_id as DayId, mealplan_id as MealPlanId, plan_date as PlanDate,
                     day_total_calories as DayTotalCalories
              FROM mealplan_days
              WHERE mealplan_id = @MealPlanId
              ORDER BY plan_date",
            new { MealPlanId = mealPlanId });

        var daysWithMeals = new List<DayWithMeals>();
        foreach (var day in days)
        {
            // Get all meals for this day
            var meals = await connection.QueryAsync<MealPlanMeal>(
                @"SELECT meal_id as MealId, day_id as DayId, meal_type as MealType,
                         calorie_target as CalorieTarget
                  FROM mealplan_meals
                  WHERE day_id = @DayId
                  ORDER BY 
                    CASE meal_type
                      WHEN 'breakfast' THEN 1
                      WHEN 'snack' THEN 2
                      WHEN 'lunch' THEN 3
                      WHEN 'dinner' THEN 4
                    END",
                new { DayId = day.DayId });

            daysWithMeals.Add(new DayWithMeals(day, meals.ToList()));
        }

        return new MealPlanDetails(plan, daysWithMeals);
    }
}

