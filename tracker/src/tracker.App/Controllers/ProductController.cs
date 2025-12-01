using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tracker.App.Services;

namespace tracker.App.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly ILogger<ProductController> _logger;
    private readonly IProductService _productService;

    public ProductController(
        ILogger<ProductController> logger,
        IProductService productService)
    {
        _logger = logger;
        _productService = productService;
    }

    /// <summary>
    /// Search for products from external API with pricing and availability
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchProducts([FromQuery] string q, [FromQuery] int limit = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest("Search term is required");
            }

            var results = await _productService.SearchProductsAsync(q, limit);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with term '{SearchTerm}'", q);
            return StatusCode(500, "An error occurred while searching for products");
        }
    }

    /// <summary>
    /// Sync a product from external API by product ID and save to local database
    /// </summary>
    [HttpPost("sync/id")]
    public async Task<IActionResult> SyncProductById([FromQuery] string productId)
    {
        try
        {
            var ingredient = await _productService.SyncProductByIdAsync(productId);
            
            if (ingredient == null)
            {
                return NotFound($"Product {productId} not found in external API");
            }

            return Ok(new 
            { 
                Message = "Product synced successfully", 
                Ingredient = ingredient 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing product {ProductId}", productId);
            return StatusCode(500, "An error occurred while syncing the product");
        }
    }

    /// <summary>
    /// Sync a product from external API by EAN barcode
    /// </summary>
    [HttpPost("sync/ean/{ean}")]
    public async Task<IActionResult> SyncProductByEan(string ean)
    {
        try
        {
            var ingredient = await _productService.SyncProductByEanAsync(ean);
            
            if (ingredient == null)
            {
                return NotFound($"Product with EAN {ean} not found in external API");
            }

            return Ok(new 
            { 
                Message = "Product synced successfully", 
                Ingredient = ingredient 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing product with EAN {Ean}", ean);
            return StatusCode(500, "An error occurred while syncing the product");
        }
    }

    /// <summary>
    /// Sync multiple products by their IDs
    /// </summary>
    [HttpPost("sync/batch")]
    public async Task<IActionResult> SyncProductsBatch([FromBody] List<string> productIds)
    {
        try
        {
            if (!productIds.Any())
            {
                return BadRequest("At least one product ID is required");
            }

            var ingredients = await _productService.SyncProductsAsync(productIds);
            
            return Ok(new 
            { 
                Message = $"Synced {ingredients.Count} of {productIds.Count} products", 
                Ingredients = ingredients 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing products batch");
            return StatusCode(500, "An error occurred while syncing products");
        }
    }

    /// <summary>
    /// Get enhanced ingredient by ID from local database
    /// </summary>
    [HttpGet("ingredient/{ingredientId:int}")]
    public async Task<IActionResult> GetEnhancedIngredient(int ingredientId)
    {
        try
        {
            var ingredient = await _productService.GetEnhancedIngredientAsync(ingredientId);
            
            if (ingredient == null)
            {
                return NotFound($"Ingredient {ingredientId} not found");
            }

            return Ok(ingredient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ingredient {IngredientId}", ingredientId);
            return StatusCode(500, "An error occurred while retrieving the ingredient");
        }
    }

    /// <summary>
    /// Get enhanced ingredient by EAN from local database
    /// </summary>
    [HttpGet("ingredient/ean/{ean}")]
    public async Task<IActionResult> GetEnhancedIngredientByEan(string ean)
    {
        try
        {
            var ingredient = await _productService.GetEnhancedIngredientByEanAsync(ean);
            
            if (ingredient == null)
            {
                return NotFound($"Ingredient with EAN {ean} not found");
            }

            return Ok(ingredient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ingredient by EAN {Ean}", ean);
            return StatusCode(500, "An error occurred while retrieving the ingredient");
        }
    }

    /// <summary>
    /// Get all enhanced ingredients from local database
    /// </summary>
    [HttpGet("ingredients")]
    public async Task<IActionResult> GetAllEnhancedIngredients()
    {
        try
        {
            var ingredients = await _productService.GetAllEnhancedIngredientsAsync();
            return Ok(ingredients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all ingredients");
            return StatusCode(500, "An error occurred while retrieving ingredients");
        }
    }

    /// <summary>
    /// Update pricing data for an ingredient
    /// </summary>
    [HttpPost("ingredient/{ingredientId:int}/update-pricing")]
    public async Task<IActionResult> UpdatePricing(int ingredientId)
    {
        try
        {
            var ingredient = await _productService.GetEnhancedIngredientAsync(ingredientId);
            
            if (ingredient == null)
            {
                return NotFound($"Ingredient {ingredientId} not found");
            }

            if (string.IsNullOrEmpty(ingredient.ExternalProductId))
            {
                return BadRequest("Ingredient does not have an external product ID");
            }

            var success = await _productService.UpdatePricingDataAsync(ingredientId, ingredient.ExternalProductId);
            
            if (!success)
            {
                return StatusCode(500, "Failed to update pricing data");
            }

            return Ok(new { Message = "Pricing data updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pricing for ingredient {IngredientId}", ingredientId);
            return StatusCode(500, "An error occurred while updating pricing data");
        }
    }

    /// <summary>
    /// Get ingredients that need price updates (older than specified days)
    /// </summary>
    [HttpGet("ingredients/stale-prices")]
    public async Task<IActionResult> GetIngredientsWithStalePrices([FromQuery] int daysOld = 7)
    {
        try
        {
            var ingredients = await _productService.GetIngredientsNeedingPriceUpdateAsync(daysOld);
            return Ok(new 
            { 
                Count = ingredients.Count, 
                Ingredients = ingredients 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ingredients with stale prices");
            return StatusCode(500, "An error occurred while retrieving ingredients");
        }
    }

    /// <summary>
    /// Refresh pricing for all ingredients with stale prices
    /// </summary>
    [HttpPost("ingredients/refresh-prices")]
    public async Task<IActionResult> RefreshStalePrices([FromQuery] int daysOld = 7)
    {
        try
        {
            var ingredients = await _productService.GetIngredientsNeedingPriceUpdateAsync(daysOld);
            var successCount = 0;

            foreach (var ingredient in ingredients)
            {
                if (!string.IsNullOrEmpty(ingredient.ExternalProductId))
                {
                    var success = await _productService.UpdatePricingDataAsync(
                        ingredient.IngredientId, 
                        ingredient.ExternalProductId);
                    
                    if (success)
                    {
                        successCount++;
                    }
                }
            }

            return Ok(new 
            { 
                Message = $"Updated {successCount} of {ingredients.Count} ingredients",
                UpdatedCount = successCount,
                TotalCount = ingredients.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing stale prices");
            return StatusCode(500, "An error occurred while refreshing prices");
        }
    }
}

