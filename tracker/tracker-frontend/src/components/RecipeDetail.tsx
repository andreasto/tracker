import { useState, useEffect } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { api, RecipeWithIngredients, ScaledRecipe } from '../services/api'

export default function RecipeDetail() {
  const navigate = useNavigate()
  const { recipeId } = useParams<{ recipeId: string }>()
  const [searchParams] = useSearchParams()
  const [recipe, setRecipe] = useState<RecipeWithIngredients | null>(null)
  const [scaledRecipe, setScaledRecipe] = useState<ScaledRecipe | null>(null)
  const [targetCalories, setTargetCalories] = useState<string>('')
  const [loading, setLoading] = useState(true)
  const [scalingLoading, setScalingLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [scalingError, setScalingError] = useState<string | null>(null)

  useEffect(() => {
    if (recipeId) {
      fetchRecipe(Number(recipeId))
    }
  }, [recipeId])

  // Auto-scale when targetCalories is in URL (coming from meal plan)
  useEffect(() => {
    const urlTargetCalories = searchParams.get('targetCalories')
    if (urlTargetCalories && recipe && !scaledRecipe) {
      setTargetCalories(urlTargetCalories)
      // Automatically trigger scaling
      const autoScale = async () => {
        setScalingLoading(true)
        setScalingError(null)
        try {
          const scaled = await api.getScaledRecipe(Number(recipeId), Number(urlTargetCalories))
          setScaledRecipe(scaled)
        } catch (err) {
          setScalingError(err instanceof Error ? err.message : 'Failed to scale recipe')
        } finally {
          setScalingLoading(false)
        }
      }
      autoScale()
    }
  }, [searchParams, recipe, recipeId, scaledRecipe])

  const fetchRecipe = async (id: number) => {
    try {
      const data = await api.getRecipeWithIngredients(id)
      setRecipe(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load recipe')
      if (err instanceof Error && err.message.includes('Authentication required')) {
        navigate('/login')
      }
    } finally {
      setLoading(false)
    }
  }

  const handleScaleRecipe = async () => {
    if (!recipeId || !targetCalories || Number(targetCalories) <= 0) {
      setScalingError('Please enter a valid calorie target')
      return
    }

    setScalingLoading(true)
    setScalingError(null)

    try {
      const scaled = await api.getScaledRecipe(Number(recipeId), Number(targetCalories))
      setScaledRecipe(scaled)
    } catch (err) {
      setScalingError(err instanceof Error ? err.message : 'Failed to scale recipe')
    } finally {
      setScalingLoading(false)
    }
  }

  const handleClearScaling = () => {
    setScaledRecipe(null)
    setTargetCalories('')
    setScalingError(null)
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading recipe...</p>
        </div>
      </div>
    )
  }

  if (error || !recipe) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="bg-red-50 border border-red-200 text-red-700 px-6 py-4 rounded-lg">
          {error || 'Recipe not found'}
        </div>
      </div>
    )
  }

  const perServingCalories = Math.round(recipe.recipe.totalCalories / recipe.recipe.servings)
  const perServingProtein = recipe.recipe.proteinG 
    ? (recipe.recipe.proteinG / recipe.recipe.servings).toFixed(1) 
    : null
  const perServingFat = recipe.recipe.fatG 
    ? (recipe.recipe.fatG / recipe.recipe.servings).toFixed(1) 
    : null
  const perServingCarbs = recipe.recipe.carbsG 
    ? (recipe.recipe.carbsG / recipe.recipe.servings).toFixed(1) 
    : null

  const isFromMealPlan = searchParams.get('targetCalories') !== null

  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto">
        <button
          onClick={() => navigate('/recipes')}
          className="mb-4 text-blue-600 hover:text-blue-800 font-medium flex items-center"
        >
          ← Back to Recipes
        </button>

        {isFromMealPlan && scaledRecipe && (
          <div className="mb-4 bg-green-50 border border-green-200 rounded-lg p-4 flex items-start">
            <span className="text-2xl mr-3">🎯</span>
            <div>
              <h3 className="font-semibold text-green-900">Auto-scaled from Meal Plan</h3>
              <p className="text-sm text-green-700">
                This recipe has been automatically scaled to match your meal plan target of {Math.round(scaledRecipe.targetCalories)} calories.
              </p>
            </div>
          </div>
        )}

        <div className="bg-white shadow rounded-lg overflow-hidden">
          {/* Header */}
          <div className="bg-gradient-to-r from-blue-500 to-blue-600 text-white p-8">
            <h1 className="text-4xl font-bold mb-2">{recipe.recipe.title}</h1>
            {recipe.recipe.description && (
              <p className="text-blue-100 text-lg">{recipe.recipe.description}</p>
            )}
            
            {/* Quick Info */}
            <div className="mt-6 flex gap-6 text-sm">
              <div className="flex items-center gap-2">
                <span className="text-2xl">🍽️</span>
                <div>
                  <div className="text-blue-100">Servings</div>
                  <div className="font-semibold">{recipe.recipe.servings}</div>
                </div>
              </div>
              {recipe.recipe.prepTimeMin && (
                <div className="flex items-center gap-2">
                  <span className="text-2xl">⏱️</span>
                  <div>
                    <div className="text-blue-100">Prep Time</div>
                    <div className="font-semibold">{recipe.recipe.prepTimeMin} min</div>
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Nutrition Information */}
          <div className="p-8 border-b border-gray-200">
            <h2 className="text-2xl font-bold text-gray-900 mb-4">Nutrition Facts</h2>
            
            {/* Total Recipe */}
            <div className="mb-6">
              <h3 className="text-sm font-semibold text-gray-600 mb-3">Total Recipe</h3>
              <div className="grid grid-cols-4 gap-4">
                <div className="bg-blue-50 rounded-lg p-4 text-center">
                  <div className="text-3xl font-bold text-blue-600">
                    {Math.round(recipe.recipe.totalCalories)}
                  </div>
                  <div className="text-sm text-gray-600 mt-1">Calories</div>
                </div>
                <div className="bg-green-50 rounded-lg p-4 text-center">
                  <div className="text-3xl font-bold text-green-600">
                    {recipe.recipe.proteinG ? Math.round(recipe.recipe.proteinG) : '-'}
                  </div>
                  <div className="text-sm text-gray-600 mt-1">Protein (g)</div>
                </div>
                <div className="bg-yellow-50 rounded-lg p-4 text-center">
                  <div className="text-3xl font-bold text-yellow-600">
                    {recipe.recipe.fatG ? Math.round(recipe.recipe.fatG) : '-'}
                  </div>
                  <div className="text-sm text-gray-600 mt-1">Fat (g)</div>
                </div>
                <div className="bg-purple-50 rounded-lg p-4 text-center">
                  <div className="text-3xl font-bold text-purple-600">
                    {recipe.recipe.carbsG ? Math.round(recipe.recipe.carbsG) : '-'}
                  </div>
                  <div className="text-sm text-gray-600 mt-1">Carbs (g)</div>
                </div>
              </div>
            </div>

            {/* Per Serving */}
            <div>
              <h3 className="text-sm font-semibold text-gray-600 mb-3">Per Serving</h3>
              <div className="grid grid-cols-4 gap-4">
                <div className="bg-gray-50 rounded-lg p-4 text-center">
                  <div className="text-2xl font-bold text-gray-700">{perServingCalories}</div>
                  <div className="text-xs text-gray-600 mt-1">Calories</div>
                </div>
                <div className="bg-gray-50 rounded-lg p-4 text-center">
                  <div className="text-2xl font-bold text-gray-700">{perServingProtein || '-'}</div>
                  <div className="text-xs text-gray-600 mt-1">Protein (g)</div>
                </div>
                <div className="bg-gray-50 rounded-lg p-4 text-center">
                  <div className="text-2xl font-bold text-gray-700">{perServingFat || '-'}</div>
                  <div className="text-xs text-gray-600 mt-1">Fat (g)</div>
                </div>
                <div className="bg-gray-50 rounded-lg p-4 text-center">
                  <div className="text-2xl font-bold text-gray-700">{perServingCarbs || '-'}</div>
                  <div className="text-xs text-gray-600 mt-1">Carbs (g)</div>
                </div>
              </div>
            </div>
          </div>

          {/* Calorie Goal Scaling */}
          <div className="p-8 border-b border-gray-200 bg-gradient-to-r from-purple-50 to-blue-50">
            <h2 className="text-2xl font-bold text-gray-900 mb-4">Scale to Your Calorie Goal</h2>
            <p className="text-gray-600 mb-4">
              Enter your target calories to see how much of each ingredient you need.
            </p>
            
            <div className="flex gap-4 items-start">
              <div className="flex-1 max-w-md">
                <label htmlFor="targetCalories" className="block text-sm font-medium text-gray-700 mb-2">
                  Target Calories
                </label>
                <input
                  id="targetCalories"
                  type="number"
                  value={targetCalories}
                  onChange={(e) => setTargetCalories(e.target.value)}
                  placeholder="e.g., 500"
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  min="1"
                />
              </div>
              <div className="flex gap-2 pt-7">
                <button
                  onClick={handleScaleRecipe}
                  disabled={scalingLoading || !targetCalories}
                  className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:bg-gray-300 disabled:cursor-not-allowed font-medium transition-colors"
                >
                  {scalingLoading ? 'Calculating...' : 'Scale Recipe'}
                </button>
                {scaledRecipe && (
                  <button
                    onClick={handleClearScaling}
                    className="px-6 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 font-medium transition-colors"
                  >
                    Clear
                  </button>
                )}
              </div>
            </div>

            {scalingError && (
              <div className="mt-4 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
                {scalingError}
              </div>
            )}

            {scaledRecipe && (
              <div className="mt-6 bg-white rounded-lg p-6 shadow-md">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-lg font-bold text-gray-900">Scaled Nutrition</h3>
                  <span className="text-sm text-gray-600">
                    Scaling Factor: <span className="font-semibold text-blue-600">{scaledRecipe.scalingFactor.toFixed(2)}x</span>
                  </span>
                </div>
                
                <div className="grid grid-cols-4 gap-4 mb-6">
                  <div className="bg-blue-50 rounded-lg p-4 text-center">
                    <div className="text-3xl font-bold text-blue-600">
                      {Math.round(scaledRecipe.actualCalories)}
                    </div>
                    <div className="text-sm text-gray-600 mt-1">Calories</div>
                  </div>
                  <div className="bg-green-50 rounded-lg p-4 text-center">
                    <div className="text-3xl font-bold text-green-600">
                      {scaledRecipe.scaledProteinG ? Math.round(scaledRecipe.scaledProteinG) : '-'}
                    </div>
                    <div className="text-sm text-gray-600 mt-1">Protein (g)</div>
                  </div>
                  <div className="bg-yellow-50 rounded-lg p-4 text-center">
                    <div className="text-3xl font-bold text-yellow-600">
                      {scaledRecipe.scaledFatG ? Math.round(scaledRecipe.scaledFatG) : '-'}
                    </div>
                    <div className="text-sm text-gray-600 mt-1">Fat (g)</div>
                  </div>
                  <div className="bg-purple-50 rounded-lg p-4 text-center">
                    <div className="text-3xl font-bold text-purple-600">
                      {scaledRecipe.scaledCarbsG ? Math.round(scaledRecipe.scaledCarbsG) : '-'}
                    </div>
                    <div className="text-sm text-gray-600 mt-1">Carbs (g)</div>
                  </div>
                </div>

                <div>
                  <h4 className="text-md font-semibold text-gray-900 mb-3">Scaled Ingredients</h4>
                  <div className="bg-gray-50 rounded-lg p-4">
                    <ul className="space-y-2">
                      {scaledRecipe.scaledIngredients.map((ingredient, index) => (
                        <li key={index} className="flex justify-between items-center py-2 border-b border-gray-200 last:border-b-0">
                          <span className="text-gray-900 font-medium">{ingredient.ingredientName}</span>
                          <div className="flex items-center gap-3">
                            <span className="text-gray-400 text-sm line-through">
                              {ingredient.originalQuantityG.toFixed(1)}g
                            </span>
                            <span className="text-blue-600 font-semibold text-lg">
                              {ingredient.scaledQuantityG.toFixed(1)}g
                            </span>
                          </div>
                        </li>
                      ))}
                    </ul>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Ingredients */}
          <div className="p-8 border-b border-gray-200">
            <h2 className="text-2xl font-bold text-gray-900 mb-4">Ingredients (Original Recipe)</h2>
            <div className="bg-gray-50 rounded-lg p-4">
              <ul className="space-y-2">
                {recipe.ingredients.map((ingredient, index) => (
                  <li key={index} className="flex justify-between items-center py-2 border-b border-gray-200 last:border-b-0">
                    <span className="text-gray-900 font-medium">{ingredient.ingredientName}</span>
                    <span className="text-gray-600">{ingredient.quantityG}g</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>

          {/* Instructions */}
          {recipe.recipe.instructions && (
            <div className="p-8 border-b border-gray-200">
              <h2 className="text-2xl font-bold text-gray-900 mb-4">Instructions</h2>
              <div className="prose max-w-none">
                <p className="text-gray-700 whitespace-pre-wrap">{recipe.recipe.instructions}</p>
              </div>
            </div>
          )}

          {/* Tags */}
          {recipe.recipe.tags && (
            <div className="p-8">
              <h2 className="text-2xl font-bold text-gray-900 mb-4">Tags</h2>
              <div className="flex flex-wrap gap-2">
                {recipe.recipe.tags.split(',').map((tag, index) => (
                  <span
                    key={index}
                    className="px-3 py-1 bg-blue-100 text-blue-700 rounded-full text-sm font-medium"
                  >
                    {tag.trim()}
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

