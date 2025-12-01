// Product API Client for Frontend
// Example usage of the product API endpoints

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

// Helper to get auth headers (from api.ts pattern)
function getAuthHeaders(): HeadersInit {
  const token = localStorage.getItem('access_token');
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
  };
  
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  
  return headers;
}

export interface EnhancedIngredient {
  ingredientId: number;
  name: string;
  externalProductId?: string;
  ean?: string;
  manufacturer?: string;
  imageUrl?: string;
  caloriesPer100G?: number;
  proteinPer100G?: number;
  fatPer100G?: number;
  carbsPer100G?: number;
  saturatedFatPer100G?: number;
  sugarPer100G?: number;
  saltPer100G?: number;
  fiberPer100G?: number;
  lowestPrice?: number;
  lowestPriceStore?: string;
  lastPriceUpdate?: string;
  lastSyncedAt?: string;
}

export interface ProviderProduct {
  productId: string;
  storeInfo?: {
    storeId: string;
    name: string;
  };
  price: number;
  date: string;
  isCampaign: boolean;
  provider: string;
  url?: string;
}

export interface ProductSearchResult {
  ingredient: EnhancedIngredient;
  availableAt: ProviderProduct[];
  averagePrice?: number;
}

export class ProductApiService {
  /**
   * Search for products from external API
   */
  async searchProducts(searchTerm: string, limit: number = 20): Promise<ProductSearchResult[]> {
    const response = await fetch(
      `${API_BASE_URL}/Product/search?q=${encodeURIComponent(searchTerm)}&limit=${limit}`,
      {
        method: 'GET',
        headers: getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to search products');
    }

    return response.json();
  }

  /**
   * Sync a product from external API by product ID
   */
  async syncProductById(productId: string): Promise<{ message: string; ingredient: EnhancedIngredient }> {
    let id = "";
    if (productId.startsWith("merged-product/")) {
      id = productId.split("merged-product/")[1];
    } else {
      id = productId;
    }
    const response = await fetch(
      `${API_BASE_URL}/product/sync/id?productId=${encodeURIComponent(id)}`,
      {
        method: 'POST',
        headers: getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to sync product');
    }

    return response.json();
  }

  /**
   * Sync a product from external API by EAN barcode
   */
  async syncProductByEan(ean: string): Promise<{ message: string; ingredient: EnhancedIngredient }> {
    const response = await fetch(
      `${API_BASE_URL}/product/sync/ean/${ean}`,
      {
        method: 'POST',
        headers: getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to sync product by EAN');
    }

    return response.json();
  }

  /**
   * Sync multiple products by their IDs
   */
  async syncProductsBatch(productIds: string[]): Promise<{ message: string; ingredients: EnhancedIngredient[] }> {
    const response = await fetch(
      `${API_BASE_URL}/product/sync/batch`,
      {
        method: 'POST',
        headers: getAuthHeaders(),
        body: JSON.stringify(productIds),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to sync products batch');
    }

    return response.json();
  }

  /**
   * Get enhanced ingredient by ID from local database
   */
  async getIngredient(ingredientId: number): Promise<EnhancedIngredient> {
    const response = await fetch(
      `${API_BASE_URL}/product/ingredient/${ingredientId}`,
      {
        method: 'GET',
        headers: getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to get ingredient');
    }

    return response.json();
  }

  /**
   * Get enhanced ingredient by EAN from local database
   */
  async getIngredientByEan(ean: string): Promise<EnhancedIngredient> {
    const response = await fetch(
      `${API_BASE_URL}/product/ingredient/ean/${ean}`,
      {
        method: 'GET',
        headers: getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to get ingredient by EAN');
    }

    return response.json();
  }

  /**
   * Get all enhanced ingredients from local database
   */
  async getAllIngredients(): Promise<EnhancedIngredient[]> {
    const response = await fetch(
      `${API_BASE_URL}/product/ingredients`,
      {
        method: 'GET',
        headers: getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to get ingredients');
    }

    return response.json();
  }

  /**
   * Update pricing data for an ingredient
   */
  async updatePricing(ingredientId: number): Promise<{ message: string }> {
    const response = await fetch(
      `${API_BASE_URL}/product/ingredient/${ingredientId}/update-pricing`,
      {
        method: 'POST',
        headers: getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to update pricing');
    }

    return response.json();
  }

  /**
   * Get ingredients with outdated pricing
   */
  async getIngredientsWithStalePrices(daysOld: number = 7): Promise<{ count: number; ingredients: EnhancedIngredient[] }> {
    const response = await fetch(
      `${API_BASE_URL}/product/ingredients/stale-prices?daysOld=${daysOld}`,
      {
        method: 'GET',
        headers: getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to get stale prices');
    }

    return response.json();
  }

  /**
   * Refresh pricing for all ingredients with stale prices
   */
  async refreshAllStalePrices(daysOld: number = 7): Promise<{ message: string; updatedCount: number; totalCount: number }> {
    const response = await fetch(
      `${API_BASE_URL}/product/ingredients/refresh-prices?daysOld=${daysOld}`,
      {
        method: 'POST',
        headers: getAuthHeaders(),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to refresh prices');
    }

    return response.json();
  }
}

// Export singleton instance
export const productApiService = new ProductApiService();

// Example Usage:
// 
// // Search for products
// const results = await productApiService.searchProducts('chicken breast', 10);
// console.log('Found products:', results);
//
// // Sync a product
// const synced = await productApiService.syncProductById('merged-product/ABC123');
// console.log('Synced ingredient:', synced.ingredient);
//
// // Get all local ingredients
// const ingredients = await productApiService.getAllIngredients();
// console.log('All ingredients:', ingredients);

