using System.Text.Json;
using tracker.Domain.MealPlan;

namespace tracker.App.Services;

public interface IProductApiClient
{
    Task<ExternalProduct?> GetProductByIdAsync(string productId, CancellationToken cancellationToken = default);
    Task<ExternalProduct?> GetProductByEanAsync(string ean, CancellationToken cancellationToken = default);
    Task<List<ExternalProduct>> SearchProductsAsync(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default);
}

public class ProductApiClient : IProductApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductApiClient(HttpClient httpClient, ILogger<ProductApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ExternalProduct?> GetProductByIdAsync(string productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/products/{productId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch product {ProductId}. Status: {StatusCode}", 
                    productId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ExternalProduct>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId} from external API", productId);
            return null;
        }
    }

    public async Task<ExternalProduct?> GetProductByEanAsync(string ean, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/products/ean/{ean}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch product by EAN {Ean}. Status: {StatusCode}", 
                    ean, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ExternalProduct>(content, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product by EAN {Ean} from external API", ean);
            return null;
        }
    }

    public async Task<List<ExternalProduct>> SearchProductsAsync(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/products?q={Uri.EscapeDataString(searchTerm)}", 
                cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to search products with term '{SearchTerm}'. Status: {StatusCode}", 
                    searchTerm, response.StatusCode);
                return new List<ExternalProduct>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResponse = JsonSerializer.Deserialize<ExternalProductSearchResponse>(content, _jsonOptions);
            return searchResponse?.Items ?? new List<ExternalProduct>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with term '{SearchTerm}' from external API", searchTerm);
            return new List<ExternalProduct>();
        }
    }
}

