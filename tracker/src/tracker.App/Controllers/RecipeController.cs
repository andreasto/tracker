using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tracker.App.Services;
using tracker.Domain.MealPlan;

namespace tracker.App.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class RecipeController : ControllerBase
{
    private readonly ILogger<RecipeController> _logger;
    private readonly IRecipeService _recipeService;

    public RecipeController(
        ILogger<RecipeController> logger,
        IRecipeService recipeService)
    {
        _logger = logger;
        _recipeService = recipeService;
    }

    // ===========================
    // RECIPE CRUD
    // ===========================

    [HttpPost]
    public async Task<IActionResult> CreateRecipe([FromBody] CreateRecipeRequest request)
    {
        try
        {
            var recipe = new Recipe
            {
                Title = request.Title,
                Description = request.Description,
                TotalCalories = request.TotalCalories,
                ProteinG = request.ProteinG,
                FatG = request.FatG,
                CarbsG = request.CarbsG,
                Servings = request.Servings,
                PrepTimeMin = request.PrepTimeMin,
                Tags = request.Tags,
                Instructions = request.Instructions
            };

            var recipeId = await _recipeService.CreateRecipeAsync(recipe, request.Ingredients);

            return Ok(new { RecipeId = recipeId, Message = "Recipe created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating recipe");
            return StatusCode(500, "An error occurred while creating the recipe");
        }
    }

    [HttpGet("{recipeId:int}")]
    public async Task<IActionResult> GetRecipe(int recipeId)
    {
        try
        {
            var recipe = await _recipeService.GetRecipeAsync(recipeId);
            if (recipe == null)
            {
                return NotFound();
            }

            return Ok(recipe);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipe {RecipeId}", recipeId);
            return StatusCode(500, "An error occurred while retrieving the recipe");
        }
    }

    [HttpGet("{recipeId:int}/details")]
    public async Task<IActionResult> GetRecipeWithIngredients(int recipeId)
    {
        try
        {
            var recipe = await _recipeService.GetRecipeWithIngredientsAsync(recipeId);
            if (recipe == null)
            {
                return NotFound();
            }

            return Ok(recipe);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipe details {RecipeId}", recipeId);
            return StatusCode(500, "An error occurred while retrieving the recipe details");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRecipes()
    {
        try
        {
            var recipes = await _recipeService.GetAllRecipesAsync();
            return Ok(recipes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all recipes");
            return StatusCode(500, "An error occurred while retrieving recipes");
        }
    }

    [HttpPut("{recipeId:int}")]
    public async Task<IActionResult> UpdateRecipe(int recipeId, [FromBody] UpdateRecipeRequest request)
    {
        try
        {
            var recipe = new Recipe
            {
                RecipeId = recipeId,
                Title = request.Title,
                Description = request.Description,
                TotalCalories = request.TotalCalories,
                ProteinG = request.ProteinG,
                FatG = request.FatG,
                CarbsG = request.CarbsG,
                Servings = request.Servings,
                PrepTimeMin = request.PrepTimeMin,
                Tags = request.Tags,
                Instructions = request.Instructions
            };

            var success = await _recipeService.UpdateRecipeAsync(recipe);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { Message = "Recipe updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating recipe {RecipeId}", recipeId);
            return StatusCode(500, "An error occurred while updating the recipe");
        }
    }

    [HttpDelete("{recipeId:int}")]
    public async Task<IActionResult> DeleteRecipe(int recipeId)
    {
        try
        {
            var success = await _recipeService.DeleteRecipeAsync(recipeId);
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { Message = "Recipe deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting recipe {RecipeId}", recipeId);
            return StatusCode(500, "An error occurred while deleting the recipe");
        }
    }

    // ===========================
    // SEARCH AND FILTER
    // ===========================

    [HttpGet("search")]
    public async Task<IActionResult> SearchRecipes(
        [FromQuery] string? searchTerm = null,
        [FromQuery] double? minCalories = null,
        [FromQuery] double? maxCalories = null,
        [FromQuery] string? tags = null)
    {
        try
        {
            var recipes = await _recipeService.SearchRecipesAsync(searchTerm, minCalories, maxCalories, tags);
            return Ok(recipes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching recipes");
            return StatusCode(500, "An error occurred while searching recipes");
        }
    }

    // ===========================
    // SCALING
    // ===========================

    [HttpGet("{recipeId:int}/scale")]
    public async Task<IActionResult> GetScaledRecipe(int recipeId, [FromQuery] double targetCalories)
    {
        try
        {
            if (targetCalories <= 0)
            {
                return BadRequest("Target calories must be greater than 0");
            }

            var scaledRecipe = await _recipeService.GetScaledRecipeAsync(recipeId, targetCalories);
            if (scaledRecipe == null)
            {
                return NotFound();
            }

            return Ok(scaledRecipe);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scaling recipe {RecipeId}", recipeId);
            return StatusCode(500, "An error occurred while scaling the recipe");
        }
    }

    [HttpGet("find-by-calories")]
    public async Task<IActionResult> FindRecipesForCalorieTarget(
        [FromQuery] double targetCalories,
        [FromQuery] double tolerance = 0.2)
    {
        try
        {
            if (targetCalories <= 0)
            {
                return BadRequest("Target calories must be greater than 0");
            }

            if (tolerance < 0 || tolerance > 1)
            {
                return BadRequest("Tolerance must be between 0 and 1");
            }

            var scaledRecipes = await _recipeService.FindRecipesForCalorieTargetAsync(targetCalories, tolerance);
            return Ok(scaledRecipes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding recipes for calorie target");
            return StatusCode(500, "An error occurred while finding recipes");
        }
    }

    // ===========================
    // INGREDIENT MANAGEMENT
    // ===========================

    [HttpPost("ingredients")]
    public async Task<IActionResult> CreateIngredient([FromBody] CreateIngredientRequest request)
    {
        try
        {
            var ingredient = new Ingredient
            {
                Name = request.Name,
                CaloriesPer100G = request.CaloriesPer100G,
                ProteinPer100G = request.ProteinPer100G,
                FatPer100G = request.FatPer100G,
                CarbsPer100G = request.CarbsPer100G
            };

            var ingredientId = await _recipeService.CreateIngredientAsync(ingredient);

            return Ok(new { IngredientId = ingredientId, Message = "Ingredient created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ingredient");
            return StatusCode(500, "An error occurred while creating the ingredient");
        }
    }

    [HttpGet("ingredients")]
    public async Task<IActionResult> GetAllIngredients()
    {
        try
        {
            var ingredients = await _recipeService.GetAllIngredientsAsync();
            return Ok(ingredients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all ingredients");
            return StatusCode(500, "An error occurred while retrieving ingredients");
        }
    }

    [HttpGet("ingredients/{ingredientId:int}")]
    public async Task<IActionResult> GetIngredient(int ingredientId)
    {
        try
        {
            var ingredient = await _recipeService.GetIngredientAsync(ingredientId);
            if (ingredient == null)
            {
                return NotFound();
            }

            return Ok(ingredient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ingredient {IngredientId}", ingredientId);
            return StatusCode(500, "An error occurred while retrieving the ingredient");
        }
    }

    // ===========================
    // MEAL PLAN RECIPE ASSIGNMENT
    // ===========================

    [HttpPost("assign-to-meal")]
    public async Task<IActionResult> AssignRecipeToMeal([FromBody] AssignRecipeToMealRequest request)
    {
        try
        {
            var success = await _recipeService.AssignRecipeToMealAsync(
                request.MealId,
                request.RecipeId,
                request.TargetCalories);

            if (!success)
            {
                return BadRequest("Failed to assign recipe to meal");
            }

            return Ok(new { Message = "Recipe assigned to meal successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning recipe to meal");
            return StatusCode(500, "An error occurred while assigning the recipe to the meal");
        }
    }

    [HttpGet("meal/{mealId:int}/recipes")]
    public async Task<IActionResult> GetRecipesForMeal(int mealId)
    {
        try
        {
            var recipes = await _recipeService.GetRecipesForMealAsync(mealId);
            return Ok(recipes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipes for meal {MealId}", mealId);
            return StatusCode(500, "An error occurred while retrieving recipes for the meal");
        }
    }
}

// ===========================
// REQUEST MODELS
// ===========================

public record CreateRecipeRequest(
    string Title,
    string? Description,
    double TotalCalories,
    double? ProteinG,
    double? FatG,
    double? CarbsG,
    int Servings,
    int? PrepTimeMin,
    string? Tags,
    string? Instructions,
    List<RecipeIngredient> Ingredients);

public record UpdateRecipeRequest(
    string Title,
    string? Description,
    double TotalCalories,
    double? ProteinG,
    double? FatG,
    double? CarbsG,
    int Servings,
    int? PrepTimeMin,
    string? Tags,
    string? Instructions);

public record CreateIngredientRequest(
    string Name,
    double? CaloriesPer100G,
    double? ProteinPer100G,
    double? FatPer100G,
    double? CarbsPer100G);

public record AssignRecipeToMealRequest(
    int MealId,
    int RecipeId,
    double TargetCalories);

