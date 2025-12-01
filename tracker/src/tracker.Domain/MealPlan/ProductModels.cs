namespace tracker.Domain.MealPlan;

/// <summary>
/// Represents a product/ingredient fetched from external API
/// </summary>
public record ExternalProduct
{
    public string Id { get; init; } = string.Empty;
    public string ProductId { get; init; } = string.Empty;
    public string? Ean { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Manufacturer { get; init; }
    public List<string> Tags { get; init; } = new();
    public ProductData? Data { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<ProviderProduct> ProviderProducts { get; init; } = new();
    public List<NutritionData> NutritionData { get; init; } = new();
}

public record ProductData
{
    public string? CategoryId { get; init; }
    public string? Image { get; init; }
    public double? Weight { get; init; }
    public string? WeightUnit { get; init; }
}

public record ProviderProduct
{
    public string ProductId { get; init; } = string.Empty;
    public StoreInfo? StoreInfo { get; init; }
    public double Price { get; init; }
    public DateTime Date { get; init; }
    public bool IsCampaign { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string? Url { get; init; }
}

public record StoreInfo
{
    public string StoreId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public record NutritionData
{
    public string Name { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public double Amount { get; init; }
}

/// <summary>
/// Enhanced ingredient model that includes external product data
/// </summary>
public record EnhancedIngredient
{
    public int IngredientId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ExternalProductId { get; init; }
    public string? Ean { get; init; }
    public string? Manufacturer { get; init; }
    public string? ImageUrl { get; init; }
    
    // Nutritional values per 100g
    public double? CaloriesPer100G { get; init; }
    public double? ProteinPer100G { get; init; }
    public double? FatPer100G { get; init; }
    public double? CarbsPer100G { get; init; }
    public double? SaturatedFatPer100G { get; init; }
    public double? SugarPer100G { get; init; }
    public double? SaltPer100G { get; init; }
    public double? FiberPer100G { get; init; }
    
    // Pricing info (from cheapest provider)
    public double? LowestPrice { get; init; }
    public string? LowestPriceStore { get; init; }
    public DateTime? LastPriceUpdate { get; init; }
    
    public DateTime? LastSyncedAt { get; init; }
}

/// <summary>
/// Search result with pricing and availability info
/// </summary>
public record ProductSearchResult
{
    public EnhancedIngredient Ingredient { get; init; } = null!;
    public List<ProviderProduct> AvailableAt { get; init; } = new();
    public double? AveragePrice { get; init; }
}

/// <summary>
/// Paginated response from the external product search API
/// </summary>
public record ExternalProductSearchResponse
{
    public List<ExternalProduct> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

