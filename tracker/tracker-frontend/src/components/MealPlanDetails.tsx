import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { api, MealPlanDetails, User } from '../services/api'

export default function MealPlanDetailsComponent() {
  const { mealPlanId } = useParams<{ mealPlanId: string }>()
  const navigate = useNavigate()
  const [details, setDetails] = useState<MealPlanDetails | null>(null)
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

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

  const { plan, days } = details
  const dailyCalorieTarget = user?.bmrWithActivityLevel 
    ? Math.round(user.bmrWithActivityLevel) 
    : null

  // Group meals by meal type for better visualization
  const getMealTypeEmoji = (mealType: string) => {
    const type = mealType.toLowerCase()
    if (type.includes('breakfast')) return '🌅'
    if (type.includes('lunch')) return '🌤️'
    if (type.includes('dinner')) return '🌙'
    if (type.includes('snack')) return '🍎'
    return '🍽️'
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
          <div className="bg-white shadow rounded-lg p-6">
            <h1 className="text-3xl font-bold text-gray-900">{plan.planName}</h1>
            <div className="mt-4 grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <p className="text-sm text-gray-600">Start Date</p>
                <p className="text-lg font-semibold">
                  {new Date(plan.startDate).toLocaleDateString()}
                </p>
              </div>
              {plan.endDate && (
                <div>
                  <p className="text-sm text-gray-600">End Date</p>
                  <p className="text-lg font-semibold">
                    {new Date(plan.endDate).toLocaleDateString()}
                  </p>
                </div>
              )}
              <div>
                <p className="text-sm text-gray-600">Target Calories/Day</p>
                <p className="text-lg font-semibold text-blue-600">
                  {dailyCalorieTarget ? `${dailyCalorieTarget} kcal` : 'N/A'}
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Days and Meals */}
        {days.length === 0 ? (
          <div className="bg-white shadow rounded-lg p-12 text-center">
            <p className="text-gray-600 text-lg mb-4">
              Your meal plan has been created! 🎉
            </p>
            <p className="text-gray-500 text-sm mb-6">
              Days and meals will be automatically generated based on your calorie targets.
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
            {days.map((dayWithMeals) => {
              const dayDate = new Date(dayWithMeals.day.date)
              const totalDayKcal = dayWithMeals.meals.reduce((sum, meal) => sum + meal.kcal, 0)
              const totalDayProtein = dayWithMeals.meals.reduce((sum, meal) => sum + meal.protein, 0)
              const totalDayCarbs = dayWithMeals.meals.reduce((sum, meal) => sum + meal.carbs, 0)
              const totalDayFat = dayWithMeals.meals.reduce((sum, meal) => sum + meal.fat, 0)

              return (
                <div key={dayWithMeals.day.mealPlanDayId} className="bg-white shadow rounded-lg p-6">
                  <div className="border-b border-gray-200 pb-4 mb-4">
                    <h2 className="text-xl font-semibold text-gray-900">
                      Day {dayWithMeals.day.dayNumber} - {dayDate.toLocaleDateString('en-US', { 
                        weekday: 'long', 
                        month: 'long', 
                        day: 'numeric' 
                      })}
                    </h2>
                    <div className="mt-2 flex flex-wrap gap-4 text-sm">
                      <div className="flex items-center">
                        <span className="text-gray-600">Total:</span>
                        <span className="ml-2 font-semibold text-blue-600">
                          {Math.round(totalDayKcal)} kcal
                        </span>
                      </div>
                      <div className="flex items-center">
                        <span className="text-gray-600">Protein:</span>
                        <span className="ml-2 font-semibold text-orange-600">
                          {Math.round(totalDayProtein)}g
                        </span>
                      </div>
                      <div className="flex items-center">
                        <span className="text-gray-600">Carbs:</span>
                        <span className="ml-2 font-semibold text-yellow-600">
                          {Math.round(totalDayCarbs)}g
                        </span>
                      </div>
                      <div className="flex items-center">
                        <span className="text-gray-600">Fat:</span>
                        <span className="ml-2 font-semibold text-green-600">
                          {Math.round(totalDayFat)}g
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="space-y-4">
                    {dayWithMeals.meals.map((meal) => (
                      <div
                        key={meal.mealPlanMealId}
                        className="border border-gray-200 rounded-lg p-4 hover:border-blue-400 transition-colors"
                      >
                        <div className="flex items-start justify-between">
                          <div className="flex-1">
                            <h3 className="font-semibold text-gray-900 flex items-center">
                              <span className="mr-2">{getMealTypeEmoji(meal.mealType)}</span>
                              {meal.recipeName}
                            </h3>
                            <p className="text-sm text-gray-600 mt-1">{meal.mealType}</p>
                          </div>
                          <div className="text-right ml-4">
                            <p className="text-lg font-bold text-blue-600">
                              {Math.round(meal.kcal)} kcal
                            </p>
                          </div>
                        </div>
                        
                        <div className="mt-3 grid grid-cols-3 gap-4 text-sm">
                          <div>
                            <span className="text-gray-600">Protein:</span>
                            <span className="ml-2 font-semibold text-orange-600">
                              {Math.round(meal.protein)}g
                            </span>
                          </div>
                          <div>
                            <span className="text-gray-600">Carbs:</span>
                            <span className="ml-2 font-semibold text-yellow-600">
                              {Math.round(meal.carbs)}g
                            </span>
                          </div>
                          <div>
                            <span className="text-gray-600">Fat:</span>
                            <span className="ml-2 font-semibold text-green-600">
                              {Math.round(meal.fat)}g
                            </span>
                          </div>
                        </div>

                        {meal.recipeId && (
                          <button className="mt-3 text-sm text-blue-600 hover:text-blue-800 font-medium">
                            View Recipe Details →
                          </button>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              )
            })}
          </div>
        )}

        {/* Nutrition Summary */}
        {days.length > 0 && (
          <div className="mt-8 bg-white shadow rounded-lg p-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">Plan Summary</h2>
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <div className="bg-blue-50 rounded-lg p-4">
                <p className="text-sm text-gray-600">Avg Daily Calories</p>
                <p className="text-2xl font-bold text-blue-600">
                  {Math.round(
                    days.reduce((sum, d) => 
                      sum + d.meals.reduce((mealSum, m) => mealSum + m.kcal, 0), 0
                    ) / days.length
                  )} kcal
                </p>
              </div>
              <div className="bg-orange-50 rounded-lg p-4">
                <p className="text-sm text-gray-600">Avg Daily Protein</p>
                <p className="text-2xl font-bold text-orange-600">
                  {Math.round(
                    days.reduce((sum, d) => 
                      sum + d.meals.reduce((mealSum, m) => mealSum + m.protein, 0), 0
                    ) / days.length
                  )}g
                </p>
              </div>
              <div className="bg-yellow-50 rounded-lg p-4">
                <p className="text-sm text-gray-600">Avg Daily Carbs</p>
                <p className="text-2xl font-bold text-yellow-600">
                  {Math.round(
                    days.reduce((sum, d) => 
                      sum + d.meals.reduce((mealSum, m) => mealSum + m.carbs, 0), 0
                    ) / days.length
                  )}g
                </p>
              </div>
              <div className="bg-green-50 rounded-lg p-4">
                <p className="text-sm text-gray-600">Avg Daily Fat</p>
                <p className="text-2xl font-bold text-green-600">
                  {Math.round(
                    days.reduce((sum, d) => 
                      sum + d.meals.reduce((mealSum, m) => mealSum + m.fat, 0), 0
                    ) / days.length
                  )}g
                </p>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

