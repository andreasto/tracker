namespace tracker.Domain.MealPlan;

public record Recipe
{
    public int RecipeId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public double TotalCalories { get; init; }
    public double? ProteinG { get; init; }
    public double? FatG { get; init; }
    public double? CarbsG { get; init; }
    public int Servings { get; init; } = 1;
    public int? PrepTimeMin { get; init; }
    public string? Tags { get; init; }
    public string? Instructions { get; init; }
}

public record Ingredient
{
    public int IngredientId { get; init; }
    public string Name { get; init; } = string.Empty;
    public double? CaloriesPer100G { get; init; }
    public double? ProteinPer100G { get; init; }
    public double? FatPer100G { get; init; }
    public double? CarbsPer100G { get; init; }
}

public record RecipeIngredient
{
    public int RecipeId { get; init; }
    public int IngredientId { get; init; }
    public double QuantityG { get; init; }
    public string IngredientName { get; init; } = string.Empty;
}

public record RecipeWithIngredients
{
    public Recipe Recipe { get; init; } = null!;
    public List<RecipeIngredient> Ingredients { get; init; } = new();
    
    /// <summary>
    /// Calculate the scaling factor needed to match a target calorie amount
    /// </summary>
    public double CalculateScalingFactor(double targetCalories)
    {
        if (Recipe.TotalCalories <= 0) return 1.0;
        return targetCalories / Recipe.TotalCalories;
    }
    
    /// <summary>
    /// Get a scaled version of this recipe to match target calories
    /// </summary>
    public ScaledRecipe ScaleToCalories(double targetCalories)
    {
        var scalingFactor = CalculateScalingFactor(targetCalories);
        return new ScaledRecipe(this, scalingFactor, targetCalories);
    }
}

public record ScaledRecipe
{
    public Recipe BaseRecipe { get; init; } = null!;
    public List<ScaledIngredient> ScaledIngredients { get; init; } = new();
    public double ScalingFactor { get; init; }
    public double TargetCalories { get; init; }
    public double ActualCalories { get; init; }
    public double? ScaledProteinG { get; init; }
    public double? ScaledFatG { get; init; }
    public double? ScaledCarbsG { get; init; }
    
    public ScaledRecipe(RecipeWithIngredients recipeWithIngredients, double scalingFactor, double targetCalories)
    {
        BaseRecipe = recipeWithIngredients.Recipe;
        ScalingFactor = scalingFactor;
        TargetCalories = targetCalories;
        ActualCalories = recipeWithIngredients.Recipe.TotalCalories * scalingFactor;
        ScaledProteinG = recipeWithIngredients.Recipe.ProteinG * scalingFactor;
        ScaledFatG = recipeWithIngredients.Recipe.FatG * scalingFactor;
        ScaledCarbsG = recipeWithIngredients.Recipe.CarbsG * scalingFactor;
        
        ScaledIngredients = recipeWithIngredients.Ingredients
            .Select(i => new ScaledIngredient(
                i.IngredientName,
                i.QuantityG,
                i.QuantityG * scalingFactor))
            .ToList();
    }
}

public record ScaledIngredient(
    string IngredientName,
    double OriginalQuantityG,
    double ScaledQuantityG);

public record MealPlanRecipe
{
    public int MealId { get; init; }
    public int RecipeId { get; init; }
    public double Servings { get; init; } = 1.0;
    public double ScalingFactor { get; init; } = 1.0;
}

