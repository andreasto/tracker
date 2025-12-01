import React, { useState } from 'react';
import { productApiService, ProductSearchResult } from '../services/productApiService';
import './IngredientSearch.css';

interface IngredientSearchProps {
  onIngredientSelected: (ingredient: any) => void;
}

export const IngredientSearch: React.FC<IngredientSearchProps> = ({ onIngredientSelected }) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [searchResults, setSearchResults] = useState<ProductSearchResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [syncing, setSyncing] = useState<string | null>(null);

  const handleSearch = async () => {
    if (!searchTerm.trim()) return;

    setLoading(true);
    setError(null);

    try {
      const results = await productApiService.searchProducts(searchTerm, 20);
      setSearchResults(results);
    } catch (err) {
      setError('Failed to search products. Please try again.');
      console.error('Search error:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSyncAndSelect = async (result: ProductSearchResult) => {
    console.log(result)
    const productId = result.ingredient.externalProductId;
    if (!productId) {
      setError('Product ID not available');
      return;
    }

    setSyncing(productId);
    setError(null);

    try {
      const synced = await productApiService.syncProductById(productId);
      onIngredientSelected(synced.ingredient);
    } catch (err) {
      setError('Failed to sync product. Please try again.');
      console.error('Sync error:', err);
    } finally {
      setSyncing(null);
    }
  };

  const formatPrice = (price?: number) => {
    if (!price) return 'N/A';
    return `${price.toFixed(2)} kr`;
  };

  const formatNutrition = (result: ProductSearchResult) => {
    const { ingredient } = result;
    const parts = [];
    
    if (ingredient.caloriesPer100G) parts.push(`${Math.round(ingredient.caloriesPer100G)} kcal`);
    if (ingredient.proteinPer100G) parts.push(`P: ${ingredient.proteinPer100G}g`);
    if (ingredient.fatPer100G) parts.push(`F: ${ingredient.fatPer100G}g`);
    if (ingredient.carbsPer100G) parts.push(`C: ${ingredient.carbsPer100G}g`);
    
    return parts.join(' | ') || 'No nutrition data';
  };

  return (
    <div className="ingredient-search">
      <div className="search-bar">
        <input
          type="text"
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
          placeholder="Search for ingredients (e.g., chicken breast, pasta)..."
          className="search-input"
        />
        <button 
          onClick={handleSearch} 
          disabled={loading || !searchTerm.trim()}
          className="search-button"
        >
          {loading ? 'Searching...' : 'Search'}
        </button>
      </div>

      {error && (
        <div className="error-message">
          {error}
        </div>
      )}

      <div className="search-results">
        {searchResults.map((result, index) => (
          <div key={index} className="product-card">
            <div className="product-info">
              {result.ingredient.imageUrl && (
                <img 
                  src={result.ingredient.imageUrl} 
                  alt={result.ingredient.name}
                  className="product-image"
                />
              )}
              
              <div className="product-details">
                <h3 className="product-name">{result.ingredient.name}</h3>
                
                {result.ingredient.manufacturer && (
                  <p className="product-manufacturer">{result.ingredient.manufacturer}</p>
                )}

                <div className="product-nutrition">
                  Per 100g: {formatNutrition(result)}
                </div>

                {result.availableAt && result.availableAt.length > 0 && (
                  <div className="product-pricing">
                    <div className="price-info">
                      <span className="price-label">Best price:</span>
                      <span className="price-value">
                        {formatPrice(result.availableAt[0].price)}
                      </span>
                      {result.availableAt[0].storeInfo && (
                        <span className="store-name">
                          at {result.availableAt[0].storeInfo.name}
                        </span>
                      )}
                    </div>
                    
                    {result.averagePrice && result.availableAt.length > 1 && (
                      <div className="avg-price">
                        Avg: {formatPrice(result.averagePrice)} ({result.availableAt.length} stores)
                      </div>
                    )}
                  </div>
                )}

                {result.ingredient.ean && (
                  <p className="product-ean">EAN: {result.ingredient.ean}</p>
                )}
              </div>
            </div>

            <button
              onClick={() => handleSyncAndSelect(result)}
              disabled={syncing === result.ingredient.externalProductId}
              className="select-button"
            >
              {syncing === result.ingredient.externalProductId ? 'Adding...' : 'Add to Recipe'}
            </button>
          </div>
        ))}
      </div>

      {searchResults.length === 0 && !loading && searchTerm && (
        <div className="no-results">
          No products found. Try a different search term.
        </div>
      )}
    </div>
  );
};


export default IngredientSearch;

