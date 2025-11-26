using Npgsql;
using Dapper;
using tracker.Domain.MealPlan;

namespace tracker.App.Services;

public interface IMealPlanService
{
    Task<MealPlanCommandResponse> CreateMealPlanAsync(
        string userId,
        string planName,
        double bmrWithActivityLevel,
        int breakfastCount,
        int lunchCount,
        int dinnerCount,
        int snackCount);
    
    Task<MealPlan?> GetMealPlanAsync(int mealPlanId);
    Task<List<MealPlan>> GetUserMealPlansAsync(string userId);
    Task<MealPlanDetails?> GetMealPlanDetailsAsync(int mealPlanId);
    Task<bool> DeleteMealPlanAsync(int mealPlanId, string userId);
}

public record MealPlanDetails(
    MealPlan Plan,
    List<MealTypeGroup> MealsByType);

public record MealTypeGroup(
    string MealType,
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
        double bmrWithActivityLevel,
        int breakfastCount,
        int lunchCount,
        int dinnerCount,
        int snackCount)
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
                    @"INSERT INTO mealplans (user_id, plan_name, total_calories)
                      VALUES (@UserId, @PlanName, @TotalCalories)
                      RETURNING mealplan_id",
                    new
                    {
                        UserId = userId,
                        PlanName = planName,
                        TotalCalories = bmrWithActivityLevel
                    },
                    transaction);

                // Create meal slots for each meal type (percentages must sum to 1.0 = 100%)
                var mealTypes = new[]
                {
                    ("breakfast", breakfastCount, 0.25),  // 25%
                    ("lunch", lunchCount, 0.35),           // 35%
                    ("dinner", dinnerCount, 0.30),         // 30%
                    ("snack", snackCount, 0.10)            // 10%
                };                                          // Total: 100% ✓

                int mealOrder = 1;
                foreach (var (mealType, count, percentage) in mealTypes)
                {
                    // Skip if no meals of this type
                    if (count == 0) continue;
                    
                    // Each meal option gets the full percentage - user chooses ONE option per day
                    var calorieTarget = bmrWithActivityLevel * percentage;

                    for (int i = 0; i < count; i++)
                    {
                        await connection.ExecuteAsync(
                            @"INSERT INTO mealplan_meals (mealplan_id, meal_type, calorie_target, meal_order)
                              VALUES (@MealPlanId, @MealType, @CalorieTarget, @MealOrder)",
                            new
                            {
                                MealPlanId = mealPlanId,
                                MealType = mealType,
                                CalorieTarget = calorieTarget,
                                MealOrder = mealOrder++
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
                     total_calories as TotalCalories, created_at as CreatedAt
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
                     total_calories as TotalCalories, created_at as CreatedAt
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

        // Get all meals for this plan with recipe information if assigned
        var mealsQuery = @"
            SELECT 
                m.meal_id as MealPlanMealId, 
                m.mealplan_id as MealPlanId,
                m.meal_type as MealType,
                m.calorie_target as CalorieTarget,
                m.meal_order as MealOrder,
                mr.recipe_id as RecipeId,
                COALESCE(r.title, 'Not Assigned') as RecipeName,
                COALESCE(r.total_calories * mr.scaling_factor, m.calorie_target) as Kcal,
                COALESCE(r.protein_g * mr.scaling_factor, 0) as Protein,
                COALESCE(r.carbs_g * mr.scaling_factor, 0) as Carbs,
                COALESCE(r.fat_g * mr.scaling_factor, 0) as Fat
            FROM mealplan_meals m
            LEFT JOIN mealplan_recipes mr ON m.meal_id = mr.meal_id
            LEFT JOIN recipes r ON mr.recipe_id = r.recipe_id
            WHERE m.mealplan_id = @MealPlanId
            ORDER BY m.meal_order";

        var meals = await connection.QueryAsync<MealPlanMeal>(mealsQuery, new { MealPlanId = mealPlanId });

        // Group meals by type
        var mealsByType = meals
            .GroupBy(m => m.MealType)
            .Select(g => new MealTypeGroup(g.Key, g.ToList()))
            .OrderBy(g => g.MealType switch
            {
                "breakfast" => 1,
                "lunch" => 2,
                "dinner" => 3,
                "snack" => 4,
                _ => 5
            })
            .ToList();

        return new MealPlanDetails(plan, mealsByType);
    }

    public async Task<bool> DeleteMealPlanAsync(int mealPlanId, string userId)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Check if the meal plan belongs to the user
            var existingPlan = await GetMealPlanAsync(mealPlanId);
            if (existingPlan == null || existingPlan.UserId != userId)
                return false; // Not found or not authorized

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Delete meal plan meals
                await connection.ExecuteAsync(
                    @"DELETE FROM mealplan_meals WHERE mealplan_id = @MealPlanId",
                    new { MealPlanId = mealPlanId },
                    transaction);

                // Delete meal plan
                await connection.ExecuteAsync(
                    @"DELETE FROM mealplans WHERE mealplan_id = @MealPlanId",
                    new { MealPlanId = mealPlanId },
                    transaction);

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch
        {
            return false;
        }
    }
}
