using Npgsql;
using Dapper;
using tracker.Domain.MealPlan;

namespace tracker.App.Services;

public interface IProductService
{
    // Sync external product data to local ingredients
    Task<EnhancedIngredient?> SyncProductByIdAsync(string productId);
    Task<EnhancedIngredient?> SyncProductByEanAsync(string ean);
    Task<List<EnhancedIngredient>> SyncProductsAsync(List<string> productIds);
    
    // Search and retrieve enhanced ingredients
    Task<List<ProductSearchResult>> SearchProductsAsync(string searchTerm, int maxResults = 20);
    Task<EnhancedIngredient?> GetEnhancedIngredientAsync(int ingredientId);
    Task<EnhancedIngredient?> GetEnhancedIngredientByEanAsync(string ean);
    Task<List<EnhancedIngredient>> GetAllEnhancedIngredientsAsync();
    
    // Price updates
    Task<bool> UpdatePricingDataAsync(int ingredientId, string externalProductId);
    Task<List<EnhancedIngredient>> GetIngredientsNeedingPriceUpdateAsync(int daysOld = 7);
}

public class ProductService : IProductService
{
    private readonly string _connectionString;
    private readonly IProductApiClient _productApiClient;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        string connectionString, 
        IProductApiClient productApiClient,
        ILogger<ProductService> logger)
    {
        _connectionString = connectionString;
        _productApiClient = productApiClient;
        _logger = logger;
    }

    public async Task<EnhancedIngredient?> SyncProductByIdAsync(string productId)
    {
        var product = await _productApiClient.GetProductByIdAsync(productId);
        if (product == null)
        {
            _logger.LogWarning("Could not fetch product {ProductId} from external API", productId);
            return null;
        }

        return await SaveOrUpdateEnhancedIngredientAsync(product);
    }

    public async Task<EnhancedIngredient?> SyncProductByEanAsync(string ean)
    {
        var product = await _productApiClient.GetProductByEanAsync(ean);
        if (product == null)
        {
            _logger.LogWarning("Could not fetch product with EAN {Ean} from external API", ean);
            return null;
        }

        return await SaveOrUpdateEnhancedIngredientAsync(product);
    }

    public async Task<List<EnhancedIngredient>> SyncProductsAsync(List<string> productIds)
    {
        var results = new List<EnhancedIngredient>();
        
        foreach (var productId in productIds)
        {
            var enhanced = await SyncProductByIdAsync(productId);
            if (enhanced != null)
            {
                results.Add(enhanced);
            }
        }

        return results;
    }

    public async Task<List<ProductSearchResult>> SearchProductsAsync(string searchTerm, int maxResults = 20)
    {
        var products = await _productApiClient.SearchProductsAsync(searchTerm, maxResults);
        var results = new List<ProductSearchResult>();

        foreach (var product in products)
        {
            // Check if we already have this ingredient synced
            var enhanced = await GetEnhancedIngredientByEanAsync(product.Ean ?? "");
            
            if (enhanced == null)
            {
                // Create a temporary enhanced ingredient from the search result
                enhanced = MapToEnhancedIngredient(product, 0);
            }

            results.Add(new ProductSearchResult
            {
                Ingredient = enhanced,
                AvailableAt = product.ProviderProducts,
                AveragePrice = product.ProviderProducts.Any() 
                    ? product.ProviderProducts.Average(p => p.Price) 
                    : null
            });
        }

        return results;
    }

    public async Task<EnhancedIngredient?> GetEnhancedIngredientAsync(int ingredientId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        
        return await connection.QueryFirstOrDefaultAsync<EnhancedIngredient>(
            @"SELECT 
                ingredient_id as IngredientId,
                name as Name,
                external_product_id as ExternalProductId,
                ean as Ean,
                manufacturer as Manufacturer,
                image_url as ImageUrl,
                calories_per_100g as CaloriesPer100G,
                protein_per_100g as ProteinPer100G,
                fat_per_100g as FatPer100G,
                carbs_per_100g as CarbsPer100G,
                saturated_fat_per_100g as SaturatedFatPer100G,
                sugar_per_100g as SugarPer100G,
                salt_per_100g as SaltPer100G,
                fiber_per_100g as FiberPer100G,
                lowest_price as LowestPrice,
                lowest_price_store as LowestPriceStore,
                last_price_update as LastPriceUpdate,
                last_synced_at as LastSyncedAt
              FROM ingredients
              WHERE ingredient_id = @IngredientId",
            new { IngredientId = ingredientId });
    }

    public async Task<EnhancedIngredient?> GetEnhancedIngredientByEanAsync(string ean)
    {
        if (string.IsNullOrWhiteSpace(ean))
            return null;

        await using var connection = new NpgsqlConnection(_connectionString);
        
        return await connection.QueryFirstOrDefaultAsync<EnhancedIngredient>(
            @"SELECT 
                ingredient_id as IngredientId,
                name as Name,
                external_product_id as ExternalProductId,
                ean as Ean,
                manufacturer as Manufacturer,
                image_url as ImageUrl,
                calories_per_100g as CaloriesPer100G,
                protein_per_100g as ProteinPer100G,
                fat_per_100g as FatPer100G,
                carbs_per_100g as CarbsPer100G,
                saturated_fat_per_100g as SaturatedFatPer100G,
                sugar_per_100g as SugarPer100G,
                salt_per_100g as SaltPer100G,
                fiber_per_100g as FiberPer100G,
                lowest_price as LowestPrice,
                lowest_price_store as LowestPriceStore,
                last_price_update as LastPriceUpdate,
                last_synced_at as LastSyncedAt
              FROM ingredients
              WHERE ean = @Ean",
            new { Ean = ean });
    }

    public async Task<List<EnhancedIngredient>> GetAllEnhancedIngredientsAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        
        var results = await connection.QueryAsync<EnhancedIngredient>(
            @"SELECT 
                ingredient_id as IngredientId,
                name as Name,
                external_product_id as ExternalProductId,
                ean as Ean,
                manufacturer as Manufacturer,
                image_url as ImageUrl,
                calories_per_100g as CaloriesPer100G,
                protein_per_100g as ProteinPer100G,
                fat_per_100g as FatPer100G,
                carbs_per_100g as CarbsPer100G,
                saturated_fat_per_100g as SaturatedFatPer100G,
                sugar_per_100g as SugarPer100G,
                salt_per_100g as SaltPer100G,
                fiber_per_100g as FiberPer100G,
                lowest_price as LowestPrice,
                lowest_price_store as LowestPriceStore,
                last_price_update as LastPriceUpdate,
                last_synced_at as LastSyncedAt
              FROM ingredients
              ORDER BY name");
              
        return results.ToList();
    }

    public async Task<bool> UpdatePricingDataAsync(int ingredientId, string externalProductId)
    {
        var product = await _productApiClient.GetProductByIdAsync(externalProductId);
        if (product == null || !product.ProviderProducts.Any())
        {
            return false;
        }

        var cheapest = product.ProviderProducts.OrderBy(p => p.Price).First();

        await using var connection = new NpgsqlConnection(_connectionString);
        
        var updated = await connection.ExecuteAsync(
            @"UPDATE ingredients 
              SET lowest_price = @Price,
                  lowest_price_store = @StoreName,
                  last_price_update = @UpdateTime
              WHERE ingredient_id = @IngredientId",
            new
            {
                IngredientId = ingredientId,
                Price = cheapest.Price,
                StoreName = cheapest.StoreInfo?.Name,
                UpdateTime = DateTime.UtcNow
            });

        return updated > 0;
    }

    public async Task<List<EnhancedIngredient>> GetIngredientsNeedingPriceUpdateAsync(int daysOld = 7)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
        
        var results = await connection.QueryAsync<EnhancedIngredient>(
            @"SELECT 
                ingredient_id as IngredientId,
                name as Name,
                external_product_id as ExternalProductId,
                ean as Ean,
                manufacturer as Manufacturer,
                image_url as ImageUrl,
                calories_per_100g as CaloriesPer100G,
                protein_per_100g as ProteinPer100G,
                fat_per_100g as FatPer100G,
                carbs_per_100g as CarbsPer100G,
                saturated_fat_per_100g as SaturatedFatPer100G,
                sugar_per_100g as SugarPer100G,
                salt_per_100g as SaltPer100G,
                fiber_per_100g as FiberPer100G,
                lowest_price as LowestPrice,
                lowest_price_store as LowestPriceStore,
                last_price_update as LastPriceUpdate,
                last_synced_at as LastSyncedAt
              FROM ingredients
              WHERE external_product_id IS NOT NULL
                AND (last_price_update IS NULL OR last_price_update < @CutoffDate)
              ORDER BY last_price_update NULLS FIRST",
            new { CutoffDate = cutoffDate });
              
        return results.ToList();
    }

    private async Task<EnhancedIngredient?> SaveOrUpdateEnhancedIngredientAsync(ExternalProduct product)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        // Extract nutrition data
        var nutritionDict = product.NutritionData.ToDictionary(n => n.Name.ToLowerInvariant(), n => n.Amount);
        
        double? GetNutritionValue(params string[] names)
        {
            foreach (var name in names)
            {
                if (nutritionDict.TryGetValue(name.ToLowerInvariant(), out var value))
                    return value;
            }
            return null;
        }

        // Convert energy from kJ to kcal (divide by 4.184)
        var energyKj = GetNutritionValue("energi", "energy");
        var calories = energyKj.HasValue ? energyKj.Value / 4.184 : (double?)null;

        var protein = GetNutritionValue("protein");
        var fat = GetNutritionValue("fett", "fat");
        var carbs = GetNutritionValue("kolhydrat", "kolhydrater", "carbohydrate", "carbohydrates");
        var saturatedFat = GetNutritionValue("varav mättat fett", "mättat fett", "saturated fat");
        var sugar = GetNutritionValue("varav sockerarter", "socker", "sugar", "sugars");
        var salt = GetNutritionValue("salt");
        var fiber = GetNutritionValue("fiber", "fibre", "kostfiber");

        // Get pricing data
        var cheapest = product.ProviderProducts.OrderBy(p => p.Price).FirstOrDefault();

        // Check if ingredient already exists by EAN
        var existingId = await connection.ExecuteScalarAsync<int?>(
            "SELECT ingredient_id FROM ingredients WHERE ean = @Ean",
            new { product.Ean });

        int ingredientId;

        if (existingId.HasValue)
        {
            // Update existing
            await connection.ExecuteAsync(
                @"UPDATE ingredients 
                  SET name = @Name,
                      external_product_id = @ExternalProductId,
                      manufacturer = @Manufacturer,
                      image_url = @ImageUrl,
                      calories_per_100g = @Calories,
                      protein_per_100g = @Protein,
                      fat_per_100g = @Fat,
                      carbs_per_100g = @Carbs,
                      saturated_fat_per_100g = @SaturatedFat,
                      sugar_per_100g = @Sugar,
                      salt_per_100g = @Salt,
                      fiber_per_100g = @Fiber,
                      lowest_price = @LowestPrice,
                      lowest_price_store = @LowestPriceStore,
                      last_price_update = @LastPriceUpdate,
                      last_synced_at = @LastSyncedAt
                  WHERE ingredient_id = @IngredientId",
                new
                {
                    IngredientId = existingId.Value,
                    Name = product.Name,
                    ExternalProductId = product.Id,
                    Manufacturer = product.Manufacturer,
                    ImageUrl = product.Data?.Image,
                    Calories = calories,
                    Protein = protein,
                    Fat = fat,
                    Carbs = carbs,
                    SaturatedFat = saturatedFat,
                    Sugar = sugar,
                    Salt = salt,
                    Fiber = fiber,
                    LowestPrice = cheapest?.Price,
                    LowestPriceStore = cheapest?.StoreInfo?.Name,
                    LastPriceUpdate = cheapest != null ? DateTime.UtcNow : (DateTime?)null,
                    LastSyncedAt = DateTime.UtcNow
                });

            ingredientId = existingId.Value;
        }
        else
        {
            // Insert new
            ingredientId = await connection.ExecuteScalarAsync<int>(
                @"INSERT INTO ingredients 
                  (name, external_product_id, ean, manufacturer, image_url, 
                   calories_per_100g, protein_per_100g, fat_per_100g, carbs_per_100g,
                   saturated_fat_per_100g, sugar_per_100g, salt_per_100g, fiber_per_100g,
                   lowest_price, lowest_price_store, last_price_update, last_synced_at)
                  VALUES 
                  (@Name, @ExternalProductId, @Ean, @Manufacturer, @ImageUrl,
                   @Calories, @Protein, @Fat, @Carbs,
                   @SaturatedFat, @Sugar, @Salt, @Fiber,
                   @LowestPrice, @LowestPriceStore, @LastPriceUpdate, @LastSyncedAt)
                  RETURNING ingredient_id",
                new
                {
                    Name = product.Name,
                    ExternalProductId = product.Id,
                    Ean = product.Ean,
                    Manufacturer = product.Manufacturer,
                    ImageUrl = product.Data?.Image,
                    Calories = calories,
                    Protein = protein,
                    Fat = fat,
                    Carbs = carbs,
                    SaturatedFat = saturatedFat,
                    Sugar = sugar,
                    Salt = salt,
                    Fiber = fiber,
                    LowestPrice = cheapest?.Price,
                    LowestPriceStore = cheapest?.StoreInfo?.Name,
                    LastPriceUpdate = cheapest != null ? DateTime.UtcNow : (DateTime?)null,
                    LastSyncedAt = DateTime.UtcNow
                });
        }

        return await GetEnhancedIngredientAsync(ingredientId);
    }

    private EnhancedIngredient MapToEnhancedIngredient(ExternalProduct product, int ingredientId)
    {
        var nutritionDict = product.NutritionData.ToDictionary(n => n.Name.ToLowerInvariant(), n => n.Amount);
        
        double? GetNutritionValue(params string[] names)
        {
            foreach (var name in names)
            {
                if (nutritionDict.TryGetValue(name.ToLowerInvariant(), out var value))
                    return value;
            }
            return null;
        }

        var energyKj = GetNutritionValue("energi", "energy");
        var calories = energyKj.HasValue ? energyKj.Value / 4.184 : (double?)null;

        var cheapest = product.ProviderProducts.OrderBy(p => p.Price).FirstOrDefault();

        return new EnhancedIngredient
        {
            IngredientId = ingredientId,
            Name = product.Name,
            ExternalProductId = product.Id,
            Ean = product.Ean,
            Manufacturer = product.Manufacturer,
            ImageUrl = product.Data?.Image,
            CaloriesPer100G = calories,
            ProteinPer100G = GetNutritionValue("protein"),
            FatPer100G = GetNutritionValue("fett", "fat"),
            CarbsPer100G = GetNutritionValue("kolhydrat", "kolhydrater", "carbohydrate", "carbohydrates"),
            SaturatedFatPer100G = GetNutritionValue("varav mättat fett", "mättat fett", "saturated fat"),
            SugarPer100G = GetNutritionValue("varav sockerarter", "socker", "sugar", "sugars"),
            SaltPer100G = GetNutritionValue("salt"),
            FiberPer100G = GetNutritionValue("fiber", "fibre", "kostfiber"),
            LowestPrice = cheapest?.Price,
            LowestPriceStore = cheapest?.StoreInfo?.Name,
            LastPriceUpdate = cheapest != null ? DateTime.UtcNow : null,
            LastSyncedAt = null
        };
    }
}

