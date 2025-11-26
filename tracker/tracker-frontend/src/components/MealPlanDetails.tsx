import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { api, MealPlanDetails, User, Recipe, ScaledRecipe, MealTypeGroup } from '../services/api'

export default function MealPlanDetailsComponent() {
  const { mealPlanId } = useParams<{ mealPlanId: string }>()
  const navigate = useNavigate()
  const [details, setDetails] = useState<MealPlanDetails | null>(null)
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  
  // Recipe selection modal state
  const [showRecipeModal, setShowRecipeModal] = useState(false)
  const [selectedMeal, setSelectedMeal] = useState<{ mealId: number; targetCalories: number } | null>(null)
  const [availableRecipes, setAvailableRecipes] = useState<Recipe[]>([])
  const [suggestedRecipes, setSuggestedRecipes] = useState<ScaledRecipe[]>([])
  const [loadingRecipes, setLoadingRecipes] = useState(false)
  const [assigningRecipe, setAssigningRecipe] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  useEffect(() => {
    const fetchData = async () => {
      if (!mealPlanId) return

      try {
        const [mealPlanDetails, userData] = await Promise.all([
          api.getMealPlanDetails(parseInt(mealPlanId)),
          api.getCurrentUser(),
        ])
        
        setDetails(mealPlanDetails)
        setUser(userData)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load meal plan')
        if (err instanceof Error && err.message.includes('Authentication required')) {
          navigate('/login')
        }
      } finally {
        setLoading(false)
      }
    }

    fetchData()
  }, [mealPlanId, navigate])

  const handleSelectRecipeForMeal = async (mealId: number, targetCalories: number) => {
    setSelectedMeal({ mealId, targetCalories })
    setShowRecipeModal(true)
    setLoadingRecipes(true)
    setError(null)
    
    try {
      // Fetch all recipes and suggested recipes for the calorie target
      const [allRecipes, suggested] = await Promise.all([
        api.getAllRecipes(),
        api.findRecipesByCalories(targetCalories, 0.3) // 30% tolerance
      ])
      
      setAvailableRecipes(allRecipes)
      setSuggestedRecipes(suggested)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load recipes')
    } finally {
      setLoadingRecipes(false)
    }
  }

  const handleAssignRecipe = async (recipeId: number, targetCalories: number) => {
    if (!selectedMeal) return
    
    setAssigningRecipe(true)
    setError(null)
    
    try {
      await api.assignRecipeToMeal({
        mealId: selectedMeal.mealId,
        recipeId,
        targetCalories
      })
      
      setSuccessMessage('Recipe assigned successfully!')
      setShowRecipeModal(false)
      
      // Refresh meal plan details
      if (mealPlanId) {
        const updatedDetails = await api.getMealPlanDetails(parseInt(mealPlanId))
        setDetails(updatedDetails)
      }
      
      setTimeout(() => setSuccessMessage(null), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to assign recipe')
    } finally {
      setAssigningRecipe(false)
    }
  }

  const closeModal = () => {
    setShowRecipeModal(false)
    setSelectedMeal(null)
    setAvailableRecipes([])
    setSuggestedRecipes([])
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading meal plan...</p>
        </div>
      </div>
    )
  }

  if (error || !details) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="bg-red-50 border border-red-200 text-red-700 px-6 py-4 rounded-lg">
            {error || 'Meal plan not found'}
          </div>
          <button
            onClick={() => navigate('/meal-plan/create')}
            className="mt-4 text-blue-600 hover:text-blue-800 font-medium"
          >
            ← Back to Meal Plans
          </button>
        </div>
      </div>
    )
  }

  const { plan, mealsByType } = details
  const dailyCalorieTarget = user?.bmrWithActivityLevel 
    ? Math.round(user.bmrWithActivityLevel) 
    : null

  // Get emoji for meal type
  const getMealTypeEmoji = (mealType: string) => {
    const type = mealType.toLowerCase()
    if (type.includes('breakfast')) return '🌅'
    if (type.includes('lunch')) return '🌤️'
    if (type.includes('dinner')) return '🌙'
    if (type.includes('snack')) return '🍎'
    return '🍽️'
  }

  const getMealTypeLabel = (mealType: string) => {
    return mealType.charAt(0).toUpperCase() + mealType.slice(1)
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4 sm:px-6 lg:px-8">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <button
            onClick={() => navigate('/meal-plan/create')}
            className="mb-4 text-blue-600 hover:text-blue-800 font-medium flex items-center"
          >
            ← Back to Meal Plans
          </button>
          
          {successMessage && (
            <div className="mb-4 bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg">
              {successMessage}
            </div>
          )}
          
          <div className="bg-white shadow rounded-lg p-6">
            <h1 className="text-3xl font-bold text-gray-900">{plan.planName}</h1>
            <div className="mt-4 grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <p className="text-sm text-gray-600">Created</p>
                <p className="text-lg font-semibold">
                  {new Date(plan.createdAt).toLocaleDateString()}
                </p>
              </div>
              <div>
                <p className="text-sm text-gray-600">Target Calories/Day</p>
                <p className="text-lg font-semibold text-blue-600">
                  {dailyCalorieTarget ? `${dailyCalorieTarget} kcal` : 'N/A'}
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Meals grouped by type */}
        {mealsByType.length === 0 ? (
          <div className="bg-white shadow rounded-lg p-12 text-center">
            <p className="text-gray-600 text-lg mb-4">
              Your meal plan has been created! 🎉
            </p>
            <p className="text-gray-500 text-sm mb-6">
              Meal slots will be generated based on your selections.
            </p>
            <button
              onClick={() => navigate('/dashboard')}
              className="px-6 py-3 bg-blue-600 text-white rounded-md hover:bg-blue-700 font-medium"
            >
              Return to Dashboard
            </button>
          </div>
        ) : (
          <div className="space-y-6">
            {mealsByType.map((mealTypeGroup) => {
              const totalKcal = mealTypeGroup.meals.reduce((sum, meal) => sum + meal.kcal, 0)
              const totalProtein = mealTypeGroup.meals.reduce((sum, meal) => sum + meal.protein, 0)
              const totalCarbs = mealTypeGroup.meals.reduce((sum, meal) => sum + meal.carbs, 0)
              const totalFat = mealTypeGroup.meals.reduce((sum, meal) => sum + meal.fat, 0)
              const avgKcal = mealTypeGroup.meals.length > 0 ? totalKcal / mealTypeGroup.meals.length : 0

              return (
                <div key={mealTypeGroup.mealType} className="bg-white shadow rounded-lg p-6">
                  <div className="border-b border-gray-200 pb-4 mb-4">
                    <h2 className="text-xl font-semibold text-gray-900 flex items-center">
                      <span className="mr-3">{getMealTypeEmoji(mealTypeGroup.mealType)}</span>
                      {getMealTypeLabel(mealTypeGroup.mealType)} Options
                      <span className="ml-3 text-sm font-normal text-gray-500">
                        ({mealTypeGroup.meals.length} recipe{mealTypeGroup.meals.length !== 1 ? 's' : ''})
                      </span>
                    </h2>
                    <div className="mt-2 flex flex-wrap gap-4 text-sm">
                      <div className="flex items-center">
                        <span className="text-gray-600">Avg per meal:</span>
                        <span className="ml-2 font-semibold text-blue-600">
                          {Math.round(avgKcal)} kcal
                        </span>
                      </div>
                      <div className="flex items-center">
                        <span className="text-gray-600">Avg Protein:</span>
                        <span className="ml-2 font-semibold text-orange-600">
                          {Math.round(totalProtein / mealTypeGroup.meals.length)}g
                        </span>
                      </div>
                      <div className="flex items-center">
                        <span className="text-gray-600">Avg Carbs:</span>
                        <span className="ml-2 font-semibold text-yellow-600">
                          {Math.round(totalCarbs / mealTypeGroup.meals.length)}g
                        </span>
                      </div>
                      <div className="flex items-center">
                        <span className="text-gray-600">Avg Fat:</span>
                        <span className="ml-2 font-semibold text-green-600">
                          {Math.round(totalFat / mealTypeGroup.meals.length)}g
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {mealTypeGroup.meals.map((meal, index) => (
                      <div
                        key={meal.mealPlanMealId}
                        className="border border-gray-200 rounded-lg p-4 hover:border-blue-400 transition-colors"
                      >
                        <div className="flex items-start justify-between mb-3">
                          <div className="flex-1">
                            <div className="flex items-center gap-2 mb-1">
                              <span className="text-xs font-medium text-gray-500 bg-gray-100 px-2 py-1 rounded">
                                Option {index + 1}
                              </span>
                            </div>
                            <h3 className="font-semibold text-gray-900">
                              {meal.recipeName}
                            </h3>
                          </div>
                          <div className="text-right ml-4">
                            <p className="text-lg font-bold text-blue-600">
                              {Math.round(meal.kcal)} kcal
                            </p>
                          </div>
                        </div>
                        
                        <div className="grid grid-cols-3 gap-2 text-xs mb-3">
                          <div>
                            <span className="text-gray-600">Protein:</span>
                            <span className="ml-1 font-semibold text-orange-600">
                              {Math.round(meal.protein)}g
                            </span>
                          </div>
                          <div>
                            <span className="text-gray-600">Carbs:</span>
                            <span className="ml-1 font-semibold text-yellow-600">
                              {Math.round(meal.carbs)}g
                            </span>
                          </div>
                          <div>
                            <span className="text-gray-600">Fat:</span>
                            <span className="ml-1 font-semibold text-green-600">
                              {Math.round(meal.fat)}g
                            </span>
                          </div>
                        </div>

                        <div className="flex gap-2 text-xs">
                          {meal.recipeId && (
                            <button 
                              onClick={() => navigate(`/recipe/${meal.recipeId}?targetCalories=${meal.kcal}`)}
                              className="text-blue-600 hover:text-blue-800 font-medium"
                            >
                              View Details →
                            </button>
                          )}
                          <button 
                            onClick={() => handleSelectRecipeForMeal(meal.mealPlanMealId, meal.kcal)}
                            className="text-green-600 hover:text-green-800 font-medium"
                          >
                            {meal.recipeId ? '🔄 Change' : '➕ Assign'}
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )
            })}
          </div>
        )}

        {/* Nutrition Summary */}
        {mealsByType.length > 0 && (
          <div className="mt-8 bg-white shadow rounded-lg p-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">Daily Nutrition Estimate</h2>
            <p className="text-sm text-gray-600 mb-4">
              Expected daily totals when choosing one recipe from each meal type
            </p>
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <div className="bg-blue-50 rounded-lg p-4">
                <p className="text-sm text-gray-600">Daily Calories</p>
                <p className="text-2xl font-bold text-blue-600">
                  {Math.round(
                    mealsByType.reduce((sum, group) => {
                      const avgKcal = group.meals.reduce((mealSum, m) => mealSum + m.kcal, 0) / group.meals.length
                      return sum + avgKcal
                    }, 0)
                  )} kcal
                </p>
                {dailyCalorieTarget && (
                  <p className="text-xs text-gray-500 mt-1">
                    Target: {dailyCalorieTarget} kcal
                  </p>
                )}
              </div>
              <div className="bg-orange-50 rounded-lg p-4">
                <p className="text-sm text-gray-600">Daily Protein</p>
                <p className="text-2xl font-bold text-orange-600">
                  {Math.round(
                    mealsByType.reduce((sum, group) => {
                      const avgProtein = group.meals.reduce((mealSum, m) => mealSum + m.protein, 0) / group.meals.length
                      return sum + avgProtein
                    }, 0)
                  )}g
                </p>
              </div>
              <div className="bg-yellow-50 rounded-lg p-4">
                <p className="text-sm text-gray-600">Daily Carbs</p>
                <p className="text-2xl font-bold text-yellow-600">
                  {Math.round(
                    mealsByType.reduce((sum, group) => {
                      const avgCarbs = group.meals.reduce((mealSum, m) => mealSum + m.carbs, 0) / group.meals.length
                      return sum + avgCarbs
                    }, 0)
                  )}g
                </p>
              </div>
              <div className="bg-green-50 rounded-lg p-4">
                <p className="text-sm text-gray-600">Daily Fat</p>
                <p className="text-2xl font-bold text-green-600">
                  {Math.round(
                    mealsByType.reduce((sum, group) => {
                      const avgFat = group.meals.reduce((mealSum, m) => mealSum + m.fat, 0) / group.meals.length
                      return sum + avgFat
                    }, 0)
                  )}g
                </p>
              </div>
            </div>
            <div className="mt-4 text-xs text-gray-500 bg-gray-50 rounded p-3">
              <p className="font-medium mb-1">💡 How this works:</p>
              <p>These numbers represent your expected daily intake when selecting one recipe from each meal type. For example, if you have 3 breakfast options averaging 600 kcal each, we count 600 kcal toward your daily total.</p>
            </div>
          </div>
        )}

        {/* Recipe Selection Modal */}
        {showRecipeModal && selectedMeal && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="bg-white rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-hidden flex flex-col">
              {/* Modal Header */}
              <div className="px-6 py-4 border-b border-gray-200">
                <div className="flex justify-between items-center">
                  <div>
                    <h2 className="text-2xl font-bold text-gray-900">Select a Recipe</h2>
                    <p className="text-sm text-gray-600 mt-1">
                      Target: {Math.round(selectedMeal.targetCalories)} calories
                    </p>
                  </div>
                  <button
                    onClick={closeModal}
                    className="text-gray-400 hover:text-gray-600 text-2xl font-bold"
                  >
                    ×
                  </button>
                </div>
              </div>

              {/* Modal Content */}
              <div className="flex-1 overflow-y-auto px-6 py-4">
                {loadingRecipes ? (
                  <div className="text-center py-12">
                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
                    <p className="mt-4 text-gray-600">Loading recipes...</p>
                  </div>
                ) : (
                  <div className="space-y-6">
                    {/* Suggested Recipes */}
                    {suggestedRecipes.length > 0 && (
                      <div>
                        <h3 className="text-lg font-semibold text-gray-900 mb-3">
                          🎯 Suggested Recipes (Best Match)
                        </h3>
                        <div className="space-y-3">
                          {suggestedRecipes.map((scaledRecipe) => (
                            <div
                              key={scaledRecipe.baseRecipe.recipeId}
                              className="border-2 border-green-200 bg-green-50 rounded-lg p-4"
                            >
                              <div className="flex justify-between items-start">
                                <div className="flex-1">
                                  <h4 className="font-semibold text-gray-900">
                                    {scaledRecipe.baseRecipe.title}
                                  </h4>
                                  <p className="text-sm text-gray-600 mt-1">
                                    Scaled by {(scaledRecipe.scalingFactor * 100).toFixed(0)}%
                                  </p>
                                  <div className="mt-2 flex gap-4 text-sm">
                                    <span className="text-blue-600 font-medium">
                                      {Math.round(scaledRecipe.actualCalories)} cal
                                    </span>
                                    {scaledRecipe.scaledProteinG && (
                                      <span className="text-orange-600">
                                        {Math.round(scaledRecipe.scaledProteinG)}g protein
                                      </span>
                                    )}
                                  </div>
                                </div>
                                <button
                                  onClick={() => handleAssignRecipe(
                                    scaledRecipe.baseRecipe.recipeId,
                                    selectedMeal.targetCalories
                                  )}
                                  disabled={assigningRecipe}
                                  className="ml-4 px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:bg-gray-400"
                                >
                                  {assigningRecipe ? 'Assigning...' : 'Assign'}
                                </button>
                              </div>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* All Recipes */}
                    <div>
                      <h3 className="text-lg font-semibold text-gray-900 mb-3">
                        All Recipes
                      </h3>
                      {availableRecipes.length === 0 ? (
                        <div className="text-center py-8 bg-gray-50 rounded-lg">
                          <p className="text-gray-600 mb-4">No recipes found.</p>
                          <button
                            onClick={() => {
                              closeModal()
                              navigate('/recipe/create')
                            }}
                            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
                          >
                            Create Your First Recipe
                          </button>
                        </div>
                      ) : (
                        <div className="space-y-3">
                          {availableRecipes.map((recipe) => (
                            <div
                              key={recipe.recipeId}
                              className="border border-gray-200 rounded-lg p-4 hover:border-blue-400 transition-colors"
                            >
                              <div className="flex justify-between items-start">
                                <div className="flex-1">
                                  <h4 className="font-semibold text-gray-900">{recipe.title}</h4>
                                  {recipe.description && (
                                    <p className="text-sm text-gray-600 mt-1 line-clamp-1">
                                      {recipe.description}
                                    </p>
                                  )}
                                  <div className="mt-2 flex gap-4 text-sm">
                                    <span className="text-blue-600 font-medium">
                                      {Math.round(recipe.totalCalories)} cal
                                    </span>
                                    {recipe.proteinG && (
                                      <span className="text-orange-600">
                                        {Math.round(recipe.proteinG)}g protein
                                      </span>
                                    )}
                                    <span className="text-gray-500">
                                      {recipe.servings} serving{recipe.servings > 1 ? 's' : ''}
                                    </span>
                                  </div>
                                </div>
                                <div className="ml-4 flex gap-2">
                                  <button
                                    onClick={() => navigate(`/recipe/${recipe.recipeId}`)}
                                    className="px-3 py-2 border border-gray-300 text-gray-700 rounded-md hover:bg-gray-50 text-sm"
                                  >
                                    View
                                  </button>
                                  <button
                                    onClick={() => handleAssignRecipe(
                                      recipe.recipeId,
                                      selectedMeal.targetCalories
                                    )}
                                    disabled={assigningRecipe}
                                    className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:bg-gray-400 text-sm"
                                  >
                                    {assigningRecipe ? 'Assigning...' : 'Assign'}
                                  </button>
                                </div>
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  </div>
                )}
              </div>

              {/* Modal Footer */}
              <div className="px-6 py-4 border-t border-gray-200 bg-gray-50">
                <div className="flex justify-between items-center">
                  <button
                    onClick={() => navigate('/recipe/create')}
                    className="text-blue-600 hover:text-blue-800 font-medium text-sm"
                  >
                    + Create New Recipe
                  </button>
                  <button
                    onClick={closeModal}
                    className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-100"
                  >
                    Close
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

