import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, Recipe } from '../services/api'

export default function RecipeList() {
  const navigate = useNavigate()
  const [recipes, setRecipes] = useState<Recipe[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [searchTerm, setSearchTerm] = useState('')
  const [filterTag, setFilterTag] = useState('')

  useEffect(() => {
    fetchRecipes()
  }, [])

  const fetchRecipes = async () => {
    try {
      const data = await api.getAllRecipes()
      setRecipes(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load recipes')
      if (err instanceof Error && err.message.includes('Authentication required')) {
        navigate('/login')
      }
    } finally {
      setLoading(false)
    }
  }

  const handleSearch = async () => {
    setLoading(true)
    setError(null)
    try {
      const data = await api.searchRecipes({
        searchTerm: searchTerm || undefined,
        tags: filterTag || undefined,
      })
      setRecipes(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to search recipes')
    } finally {
      setLoading(false)
    }
  }

  const handleReset = () => {
    setSearchTerm('')
    setFilterTag('')
    fetchRecipes()
  }

  const handleViewRecipe = (recipeId: number) => {
    navigate(`/recipe/${recipeId}`)
  }

  if (loading && recipes.length === 0) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading recipes...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4 sm:px-6 lg:px-8">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <button
            onClick={() => navigate('/dashboard')}
            className="mb-4 text-blue-600 hover:text-blue-800 font-medium flex items-center"
          >
            ← Back to Dashboard
          </button>
          <div className="flex justify-between items-center">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">Recipes</h1>
              <p className="mt-2 text-gray-600">Browse and manage your recipe collection</p>
            </div>
            <button
              onClick={() => navigate('/recipe/create')}
              className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 font-medium"
            >
              + Create Recipe
            </button>
          </div>
        </div>

        {/* Search and Filter */}
        <div className="bg-white shadow rounded-lg p-4 mb-6">
          <div className="flex gap-4">
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
              placeholder="Search recipes..."
              className="flex-1 px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <input
              type="text"
              value={filterTag}
              onChange={(e) => setFilterTag(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
              placeholder="Filter by tag..."
              className="w-48 px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <button
              onClick={handleSearch}
              className="px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
            >
              Search
            </button>
            <button
              onClick={handleReset}
              className="px-6 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50"
            >
              Reset
            </button>
          </div>
        </div>

        {error && (
          <div className="mb-6 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
            {error}
          </div>
        )}

        {/* Recipe Grid */}
        {recipes.length === 0 ? (
          <div className="bg-white shadow rounded-lg p-12 text-center">
            <div className="text-gray-400 text-6xl mb-4">🍳</div>
            <h3 className="text-xl font-semibold text-gray-900 mb-2">No recipes found</h3>
            <p className="text-gray-600 mb-6">
              {searchTerm || filterTag
                ? 'Try adjusting your search criteria'
                : 'Get started by creating your first recipe'}
            </p>
            <button
              onClick={() => navigate('/recipe/create')}
              className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 font-medium"
            >
              Create Your First Recipe
            </button>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {recipes.map((recipe) => (
              <div
                key={recipe.recipeId}
                className="bg-white shadow rounded-lg overflow-hidden hover:shadow-lg transition-shadow cursor-pointer"
                onClick={() => handleViewRecipe(recipe.recipeId)}
              >
                <div className="p-6">
                  <h3 className="text-xl font-semibold text-gray-900 mb-2">{recipe.title}</h3>
                  
                  {recipe.description && (
                    <p className="text-gray-600 text-sm mb-4 line-clamp-2">{recipe.description}</p>
                  )}

                  {/* Nutrition Info */}
                  <div className="grid grid-cols-2 gap-3 mb-4">
                    <div className="bg-blue-50 rounded-lg p-3">
                      <div className="text-sm text-gray-600">Calories</div>
                      <div className="text-lg font-bold text-blue-600">
                        {Math.round(recipe.totalCalories)}
                      </div>
                    </div>
                    <div className="bg-green-50 rounded-lg p-3">
                      <div className="text-sm text-gray-600">Protein</div>
                      <div className="text-lg font-bold text-green-600">
                        {recipe.proteinG ? Math.round(recipe.proteinG) : '-'}g
                      </div>
                    </div>
                  </div>

                  {/* Meta Info */}
                  <div className="flex justify-between items-center text-sm text-gray-600">
                    <div>
                      <span className="font-medium">{recipe.servings}</span> serving{recipe.servings > 1 ? 's' : ''}
                    </div>
                    {recipe.prepTimeMin && (
                      <div>
                        ⏱️ {recipe.prepTimeMin} min
                      </div>
                    )}
                  </div>

                  {/* Tags */}
                  {recipe.tags && (
                    <div className="mt-3 flex flex-wrap gap-2">
                      {recipe.tags.split(',').map((tag, index) => (
                        <span
                          key={index}
                          className="px-2 py-1 bg-gray-100 text-gray-700 text-xs rounded-full"
                        >
                          {tag.trim()}
                        </span>
                      ))}
                    </div>
                  )}
                </div>

                <div className="bg-gray-50 px-6 py-3 border-t border-gray-200">
                  <button
                    onClick={(e) => {
                      e.stopPropagation()
                      handleViewRecipe(recipe.recipeId)
                    }}
                    className="text-blue-600 hover:text-blue-800 text-sm font-medium"
                  >
                    View Details →
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

