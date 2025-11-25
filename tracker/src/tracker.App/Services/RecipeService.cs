using Npgsql;
using Dapper;
using tracker.Domain.MealPlan;

namespace tracker.App.Services;

public interface IRecipeService
{
    // Recipe CRUD
    Task<int> CreateRecipeAsync(Recipe recipe, List<RecipeIngredient> ingredients);
    Task<Recipe?> GetRecipeAsync(int recipeId);
    Task<List<Recipe>> GetAllRecipesAsync();
    Task<RecipeWithIngredients?> GetRecipeWithIngredientsAsync(int recipeId);
    Task<bool> UpdateRecipeAsync(Recipe recipe);
    Task<bool> DeleteRecipeAsync(int recipeId);
    
    // Search and filter
    Task<List<Recipe>> SearchRecipesAsync(string? searchTerm = null, double? minCalories = null, double? maxCalories = null, string? tags = null);
    Task<List<Recipe>> GetRecipesByCalorieRangeAsync(double minCalories, double maxCalories);
    
    // Scaling
    Task<ScaledRecipe?> GetScaledRecipeAsync(int recipeId, double targetCalories);
    Task<List<ScaledRecipe>> FindRecipesForCalorieTargetAsync(double targetCalories, double tolerance = 0.2);
    
    // Ingredient management
    Task<int> CreateIngredientAsync(Ingredient ingredient);
    Task<Ingredient?> GetIngredientAsync(int ingredientId);
    Task<List<Ingredient>> GetAllIngredientsAsync();
    Task<Ingredient?> GetIngredientByNameAsync(string name);
    
    // Meal plan recipe assignment
    Task<bool> AssignRecipeToMealAsync(int mealId, int recipeId, double targetCalories);
    Task<List<RecipeWithScaling>> GetRecipesForMealAsync(int mealId);
}

public record RecipeWithScaling(
    Recipe Recipe,
    double ScalingFactor,
    double ScaledCalories);

public class RecipeService : IRecipeService
{
    private readonly string _connectionString;

    public RecipeService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<int> CreateRecipeAsync(Recipe recipe, List<RecipeIngredient> ingredients)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Insert recipe
            var recipeId = await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO recipes (title, description, total_calories, protein_g, fat_g, carbs_g, servings, prep_time_min, tags, instructions)
                  VALUES (@Title, @Description, @TotalCalories, @ProteinG, @FatG, @CarbsG, @Servings, @PrepTimeMin, @Tags, @Instructions)
                  RETURNING recipe_id",
                new
                {
                    recipe.Title,
                    recipe.Description,
                    recipe.TotalCalories,
                    recipe.ProteinG,
                    recipe.FatG,
                    recipe.CarbsG,
                    recipe.Servings,
                    recipe.PrepTimeMin,
                    recipe.Tags,
                    recipe.Instructions
                },
                transaction);

            // Insert recipe ingredients
            foreach (var ingredient in ingredients)
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO recipe_ingredients (recipe_id, ingredient_id, quantity_g)
                      VALUES (@RecipeId, @IngredientId, @QuantityG)",
                    new
                    {
                        RecipeId = recipeId,
                        ingredient.IngredientId,
                        ingredient.QuantityG
                    },
                    transaction);
            }

            await transaction.CommitAsync();
            return recipeId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Recipe?> GetRecipeAsync(int recipeId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        return await connection.QuerySingleOrDefaultAsync<Recipe>(
            @"SELECT recipe_id as RecipeId, title as Title, description as Description,
                     total_calories as TotalCalories, protein_g as ProteinG, fat_g as FatG,
                     carbs_g as CarbsG, servings as Servings, prep_time_min as PrepTimeMin,
                     tags as Tags, instructions as Instructions
              FROM recipes
              WHERE recipe_id = @RecipeId",
            new { RecipeId = recipeId });
    }

    public async Task<List<Recipe>> GetAllRecipesAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var result = await connection.QueryAsync<Recipe>(
            @"SELECT recipe_id as RecipeId, title as Title, description as Description,
                     total_calories as TotalCalories, protein_g as ProteinG, fat_g as FatG,
                     carbs_g as CarbsG, servings as Servings, prep_time_min as PrepTimeMin,
                     tags as Tags, instructions as Instructions
              FROM recipes
              ORDER BY title");

        return result.ToList();
    }

    public async Task<RecipeWithIngredients?> GetRecipeWithIngredientsAsync(int recipeId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var recipe = await GetRecipeAsync(recipeId);
        if (recipe == null) return null;

        var ingredients = await connection.QueryAsync<RecipeIngredient>(
            @"SELECT ri.recipe_id as RecipeId, ri.ingredient_id as IngredientId,
                     ri.quantity_g as QuantityG, i.name as IngredientName
              FROM recipe_ingredients ri
              JOIN ingredients i ON ri.ingredient_id = i.ingredient_id
              WHERE ri.recipe_id = @RecipeId",
            new { RecipeId = recipeId });

        return new RecipeWithIngredients
        {
            Recipe = recipe,
            Ingredients = ingredients.ToList()
        };
    }

    public async Task<bool> UpdateRecipeAsync(Recipe recipe)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var rowsAffected = await connection.ExecuteAsync(
            @"UPDATE recipes
              SET title = @Title, description = @Description, total_calories = @TotalCalories,
                  protein_g = @ProteinG, fat_g = @FatG, carbs_g = @CarbsG,
                  servings = @Servings, prep_time_min = @PrepTimeMin,
                  tags = @Tags, instructions = @Instructions
              WHERE recipe_id = @RecipeId",
            new
            {
                recipe.RecipeId,
                recipe.Title,
                recipe.Description,
                recipe.TotalCalories,
                recipe.ProteinG,
                recipe.FatG,
                recipe.CarbsG,
                recipe.Servings,
                recipe.PrepTimeMin,
                recipe.Tags,
                recipe.Instructions
            });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteRecipeAsync(int recipeId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var rowsAffected = await connection.ExecuteAsync(
            "DELETE FROM recipes WHERE recipe_id = @RecipeId",
            new { RecipeId = recipeId });

        return rowsAffected > 0;
    }

    public async Task<List<Recipe>> SearchRecipesAsync(string? searchTerm = null, double? minCalories = null, double? maxCalories = null, string? tags = null)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"SELECT recipe_id as RecipeId, title as Title, description as Description,
                           total_calories as TotalCalories, protein_g as ProteinG, fat_g as FatG,
                           carbs_g as CarbsG, servings as Servings, prep_time_min as PrepTimeMin,
                           tags as Tags, instructions as Instructions
                    FROM recipes
                    WHERE 1=1";

        if (!string.IsNullOrEmpty(searchTerm))
        {
            sql += " AND (title ILIKE @SearchTerm OR description ILIKE @SearchTerm)";
        }

        if (minCalories.HasValue)
        {
            sql += " AND total_calories >= @MinCalories";
        }

        if (maxCalories.HasValue)
        {
            sql += " AND total_calories <= @MaxCalories";
        }

        if (!string.IsNullOrEmpty(tags))
        {
            sql += " AND tags ILIKE @Tags";
        }

        sql += " ORDER BY title";

        var result = await connection.QueryAsync<Recipe>(sql, new
        {
            SearchTerm = $"%{searchTerm}%",
            MinCalories = minCalories,
            MaxCalories = maxCalories,
            Tags = $"%{tags}%"
        });

        return result.ToList();
    }

    public async Task<List<Recipe>> GetRecipesByCalorieRangeAsync(double minCalories, double maxCalories)
    {
        return await SearchRecipesAsync(minCalories: minCalories, maxCalories: maxCalories);
    }

    public async Task<ScaledRecipe?> GetScaledRecipeAsync(int recipeId, double targetCalories)
    {
        var recipeWithIngredients = await GetRecipeWithIngredientsAsync(recipeId);
        if (recipeWithIngredients == null) return null;

        return recipeWithIngredients.ScaleToCalories(targetCalories);
    }

    public async Task<List<ScaledRecipe>> FindRecipesForCalorieTargetAsync(double targetCalories, double tolerance = 0.2)
    {
        // Find recipes within the tolerance range
        var minCalories = targetCalories * (1 - tolerance);
        var maxCalories = targetCalories * (1 + tolerance);

        var recipes = await GetRecipesByCalorieRangeAsync(minCalories, maxCalories);
        var scaledRecipes = new List<ScaledRecipe>();

        foreach (var recipe in recipes)
        {
            var recipeWithIngredients = await GetRecipeWithIngredientsAsync(recipe.RecipeId);
            if (recipeWithIngredients != null)
            {
                scaledRecipes.Add(recipeWithIngredients.ScaleToCalories(targetCalories));
            }
        }

        // Sort by how close the scaling factor is to 1.0 (less scaling = better match)
        return scaledRecipes
            .OrderBy(sr => Math.Abs(sr.ScalingFactor - 1.0))
            .ToList();
    }

    public async Task<int> CreateIngredientAsync(Ingredient ingredient)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var ingredientId = await connection.ExecuteScalarAsync<int>(
            @"INSERT INTO ingredients (name, calories_per_100g, protein_per_100g, fat_per_100g, carbs_per_100g)
              VALUES (@Name, @CaloriesPer100G, @ProteinPer100G, @FatPer100G, @CarbsPer100G)
              RETURNING ingredient_id",
            new
            {
                ingredient.Name,
                ingredient.CaloriesPer100G,
                ingredient.ProteinPer100G,
                ingredient.FatPer100G,
                ingredient.CarbsPer100G
            });

        return ingredientId;
    }

    public async Task<Ingredient?> GetIngredientAsync(int ingredientId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        return await connection.QuerySingleOrDefaultAsync<Ingredient>(
            @"SELECT ingredient_id as IngredientId, name as Name,
                     calories_per_100g as CaloriesPer100G, protein_per_100g as ProteinPer100G,
                     fat_per_100g as FatPer100G, carbs_per_100g as CarbsPer100G
              FROM ingredients
              WHERE ingredient_id = @IngredientId",
            new { IngredientId = ingredientId });
    }

    public async Task<List<Ingredient>> GetAllIngredientsAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var result = await connection.QueryAsync<Ingredient>(
            @"SELECT ingredient_id as IngredientId, name as Name,
                     calories_per_100g as CaloriesPer100G, protein_per_100g as ProteinPer100G,
                     fat_per_100g as FatPer100G, carbs_per_100g as CarbsPer100G
              FROM ingredients
              ORDER BY name");

        return result.ToList();
    }

    public async Task<Ingredient?> GetIngredientByNameAsync(string name)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        return await connection.QuerySingleOrDefaultAsync<Ingredient>(
            @"SELECT ingredient_id as IngredientId, name as Name,
                     calories_per_100g as CaloriesPer100G, protein_per_100g as ProteinPer100G,
                     fat_per_100g as FatPer100G, carbs_per_100g as CarbsPer100G
              FROM ingredients
              WHERE LOWER(name) = LOWER(@Name)",
            new { Name = name });
    }

    public async Task<bool> AssignRecipeToMealAsync(int mealId, int recipeId, double targetCalories)
    {
        var recipeWithIngredients = await GetRecipeWithIngredientsAsync(recipeId);
        if (recipeWithIngredients == null) return false;

        var scalingFactor = recipeWithIngredients.CalculateScalingFactor(targetCalories);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var rowsAffected = await connection.ExecuteAsync(
            @"INSERT INTO mealplan_recipes (meal_id, recipe_id, servings, scaling_factor)
              VALUES (@MealId, @RecipeId, @Servings, @ScalingFactor)
              ON CONFLICT (meal_id, recipe_id) 
              DO UPDATE SET servings = @Servings, scaling_factor = @ScalingFactor",
            new
            {
                MealId = mealId,
                RecipeId = recipeId,
                Servings = scalingFactor, // Use scaling factor as servings for now
                ScalingFactor = scalingFactor
            });

        return rowsAffected > 0;
    }

    public async Task<List<RecipeWithScaling>> GetRecipesForMealAsync(int mealId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"SELECT r.recipe_id as RecipeId, r.title as Title, r.description as Description,
                     r.total_calories as TotalCalories, r.protein_g as ProteinG, r.fat_g as FatG,
                     r.carbs_g as CarbsG, r.servings as Servings, r.prep_time_min as PrepTimeMin,
                     r.tags as Tags, r.instructions as Instructions,
                     mr.scaling_factor as ScalingFactor,
                     (r.total_calories * mr.scaling_factor) as ScaledCalories
              FROM mealplan_recipes mr
              JOIN recipes r ON mr.recipe_id = r.recipe_id
              WHERE mr.meal_id = @MealId";

        var results = await connection.QueryAsync(sql, new { MealId = mealId });
        
        var recipeList = new List<RecipeWithScaling>();
        foreach (var row in results)
        {
            recipeList.Add(new RecipeWithScaling(
                Recipe: new Recipe
                {
                    RecipeId = row.RecipeId,
                    Title = row.Title,
                    Description = row.Description,
                    TotalCalories = row.TotalCalories,
                    ProteinG = row.ProteinG,
                    FatG = row.FatG,
                    CarbsG = row.CarbsG,
                    Servings = row.Servings,
                    PrepTimeMin = row.PrepTimeMin,
                    Tags = row.Tags,
                    Instructions = row.Instructions
                },
                ScalingFactor: row.ScalingFactor,
                ScaledCalories: row.ScaledCalories
            ));
        }

        return recipeList;
    }
}

