import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, User, MealPlan, UserOnboardingState } from '../services/api'

export default function MealPlanCreator() {
  const navigate = useNavigate()
  const [user, setUser] = useState<User | null>(null)
  const [existingPlans, setExistingPlans] = useState<MealPlan[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  
  // Form state
  const [planName, setPlanName] = useState('')
  const [breakfastCount, setBreakfastCount] = useState(3)
  const [lunchCount, setLunchCount] = useState(3)
  const [dinnerCount, setDinnerCount] = useState(3)
  const [snackCount, setSnackCount] = useState(2)

  useEffect(() => {
    const fetchData = async () => {
      try {
        const userData = await api.getCurrentUser()
        setUser(userData)
        
        // Fetch existing meal plans
        const plans = await api.getUserMealPlans(userData.userId)
        setExistingPlans(plans)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load data')
        if (err instanceof Error && err.message.includes('Authentication required')) {
          navigate('/login')
        }
      } finally {
        setLoading(false)
      }
    }

    fetchData()
  }, [navigate])

  const handleCreateMealPlan = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!user) return
    
    if (!planName.trim()) {
      setError('Please enter a meal plan name')
      return
    }
    
    if (breakfastCount + lunchCount + dinnerCount + snackCount === 0) {
      setError('Please select at least one meal')
      return
    }
    
    console.log('User onboarding state:', typeof user.onboardingState)
    if (user.onboardingState !== UserOnboardingState.Complete) {
      setError('You must complete onboarding before creating a meal plan')
      return
    }

    if (!user.bmrWithActivityLevel) {
      setError('Your BMR is not calculated. Please complete your profile.')
      return
    }

    setCreating(true)
    setError(null)

    try {
      const result = await api.createMealPlan(user.userId, {
        planName: planName.trim(),
        breakfastCount,
        lunchCount,
        dinnerCount,
        snackCount,
      })

      // Navigate to the meal plan details page
      navigate(`/meal-plan/${result.mealPlanId}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create meal plan')
      setCreating(false)
    }
  }

  const handleViewPlan = (mealPlanId: number) => {
    navigate(`/meal-plan/${mealPlanId}`)
  }

  const handleDeletePlan = async (mealPlanId: number, planName: string, e: React.MouseEvent) => {
    e.stopPropagation() // Prevent card click from triggering
    
    if (!window.confirm(`Are you sure you want to delete "${planName}"? This action cannot be undone.`)) {
      return
    }

    try {
      await api.deleteMealPlan(mealPlanId)
      
      // Remove from local state
      setExistingPlans(existingPlans.filter(p => p.mealPlanId !== mealPlanId))
      
      // Show success message briefly
      setError(null)
      const successDiv = document.createElement('div')
      successDiv.className = 'fixed top-4 right-4 bg-green-50 border border-green-200 text-green-700 px-6 py-3 rounded-lg shadow-lg z-50'
      successDiv.textContent = 'Meal plan deleted successfully'
      document.body.appendChild(successDiv)
      setTimeout(() => successDiv.remove(), 3000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete meal plan')
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading...</p>
        </div>
      </div>
    )
  }

  if (!user) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="bg-red-50 border border-red-200 text-red-700 px-6 py-4 rounded-lg">
          User not found
        </div>
      </div>
    )
  }

  const mealsPerDay = user.questionnaireAnswers?.mealsPerDay || '3'
  const dailyCalorieTarget = user.bmrWithActivityLevel 
    ? Math.round(user.bmrWithActivityLevel) 
    : null

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
          <h1 className="text-3xl font-bold text-gray-900">Create Meal Plan</h1>
          <p className="mt-2 text-gray-600">
            Design your personalized meal plan based on your goals and calorie needs
          </p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Left Column - Create New Plan */}
          <div className="lg:col-span-2">
            <div className="bg-white shadow rounded-lg p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">New Meal Plan</h2>
              
              {error && (
                <div className="mb-6 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
                  {error}
                </div>
              )}

              {/* User's Calorie Info */}
              <div className="mb-6 bg-blue-50 border-2 border-blue-200 rounded-lg p-4">
                <h3 className="font-semibold text-gray-900 mb-3 flex items-center">
                  🎯 Your Daily Targets
                </h3>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <p className="text-sm text-gray-600">Daily Calories</p>
                    <p className="text-2xl font-bold text-blue-600">
                      {dailyCalorieTarget ? `${dailyCalorieTarget} kcal` : 'Not calculated'}
                    </p>
                  </div>
                  <div>
                    <p className="text-sm text-gray-600">Total Meals Selected</p>
                    <p className="text-2xl font-bold text-green-600">
                      {breakfastCount + lunchCount + dinnerCount + snackCount} meals
                    </p>
                  </div>
                </div>
                {dailyCalorieTarget && (
                  <p className="mt-3 text-xs text-gray-600">
                    💡 Create a rotating meal plan with your chosen number of recipes for each meal type
                  </p>
                )}
              </div>

              {/* Create Form */}
              <form onSubmit={handleCreateMealPlan}>
                <div className="space-y-4">
                  <div>
                    <label htmlFor="planName" className="block text-sm font-medium text-gray-700">
                      Plan Name *
                    </label>
                    <input
                      type="text"
                      id="planName"
                      value={planName}
                      onChange={(e) => setPlanName(e.target.value)}
                      placeholder="e.g., Winter Cut Plan, Muscle Gain 2024"
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-4 py-2 border"
                      required
                    />
                  </div>

                  <div className="space-y-3">
                    <h3 className="text-sm font-semibold text-gray-900 mt-4">
                      Select Number of Recipes per Meal Type
                    </h3>
                    <p className="text-xs text-gray-600">
                      Choose how many different recipes you want for each meal type in your rotation
                    </p>

                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label htmlFor="breakfastCount" className="block text-sm font-medium text-gray-700">
                          🌅 Breakfast Recipes
                        </label>
                        <input
                          type="number"
                          id="breakfastCount"
                          value={breakfastCount}
                          onChange={(e) => setBreakfastCount(Math.max(0, parseInt(e.target.value) || 0))}
                          min="0"
                          max="20"
                          className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-4 py-2 border"
                        />
                      </div>

                      <div>
                        <label htmlFor="lunchCount" className="block text-sm font-medium text-gray-700">
                          🌤️ Lunch Recipes
                        </label>
                        <input
                          type="number"
                          id="lunchCount"
                          value={lunchCount}
                          onChange={(e) => setLunchCount(Math.max(0, parseInt(e.target.value) || 0))}
                          min="0"
                          max="20"
                          className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-4 py-2 border"
                        />
                      </div>

                      <div>
                        <label htmlFor="dinnerCount" className="block text-sm font-medium text-gray-700">
                          🌙 Dinner Recipes
                        </label>
                        <input
                          type="number"
                          id="dinnerCount"
                          value={dinnerCount}
                          onChange={(e) => setDinnerCount(Math.max(0, parseInt(e.target.value) || 0))}
                          min="0"
                          max="20"
                          className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-4 py-2 border"
                        />
                      </div>

                      <div>
                        <label htmlFor="snackCount" className="block text-sm font-medium text-gray-700">
                          🍎 Snack Recipes
                        </label>
                        <input
                          type="number"
                          id="snackCount"
                          value={snackCount}
                          onChange={(e) => setSnackCount(Math.max(0, parseInt(e.target.value) || 0))}
                          min="0"
                          max="20"
                          className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-4 py-2 border"
                        />
                      </div>
                    </div>
                  </div>

                  <div className="pt-4">
                    <button
                      type="submit"
                      disabled={creating || !dailyCalorieTarget}
                      className="w-full flex justify-center py-3 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {creating ? (
                        <>
                          <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-white mr-2"></div>
                          Creating Meal Plan...
                        </>
                      ) : (
                        '🍽️ Create Meal Plan'
                      )}
                    </button>
                  </div>

                  {!dailyCalorieTarget && (
                    <p className="text-sm text-red-600 text-center">
                      Please complete your onboarding to calculate your daily calorie needs
                    </p>
                  )}
                </div>
              </form>

              {/* Information Box */}
              <div className="mt-6 bg-gray-50 border border-gray-200 rounded-lg p-4">
                <h4 className="font-medium text-gray-900 mb-2">📋 What happens next?</h4>
                <ul className="text-sm text-gray-600 space-y-1 ml-4 list-disc">
                  <li>A meal plan will be created with slots for your selected meal types</li>
                  <li>Each meal will be targeted to fit your daily {dailyCalorieTarget || '...'} kcal goal</li>
                  <li>You can then select and customize recipes for each meal slot</li>
                  <li>The system will help balance your macros (protein, carbs, fat)</li>
                  <li>Rotate through your recipes for variety throughout the week</li>
                </ul>
              </div>
            </div>
          </div>

          {/* Right Column - Existing Plans */}
          <div className="lg:col-span-1">
            <div className="bg-white shadow rounded-lg p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">Your Meal Plans</h2>
              
              {existingPlans.length === 0 ? (
                <div className="text-center py-8">
                  <p className="text-gray-500 text-sm">No meal plans yet</p>
                  <p className="text-gray-400 text-xs mt-2">Create your first one! 🎉</p>
                </div>
              ) : (
                <div className="space-y-3">
                  {existingPlans.map((plan) => (
                    <div
                      key={plan.mealPlanId}
                      className="border border-gray-200 rounded-lg p-4 hover:border-blue-400 hover:shadow-md transition-all cursor-pointer relative"
                      onClick={() => handleViewPlan(plan.mealPlanId)}
                    >
                      <div className="flex justify-between items-start">
                        <div className="flex-1">
                          <h3 className="font-semibold text-gray-900 mb-1">{plan.planName}</h3>
                          <p className="text-xs text-gray-500">
                            Created: {new Date(plan.createdAt).toLocaleDateString()}
                          </p>
                          {plan.totalKcal && (
                            <div className="mt-2 pt-2 border-t border-gray-100">
                              <p className="text-xs font-medium text-blue-600">
                                Target: {Math.round(plan.totalKcal)} kcal/day
                              </p>
                            </div>
                          )}
                        </div>
                        <button
                          onClick={(e) => handleDeletePlan(plan.mealPlanId, plan.planName, e)}
                          className="ml-2 p-2 text-red-600 hover:text-red-800 hover:bg-red-50 rounded-lg transition-colors"
                          title="Delete meal plan"
                        >
                          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                            <path fillRule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clipRule="evenodd" />
                          </svg>
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Quick Stats */}
            {user.questionnaireAnswers && (
              <div className="mt-6 bg-white shadow rounded-lg p-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">Your Goals</h3>
                <dl className="space-y-2">
                  {Object.entries(user.questionnaireAnswers).map(([key, value]) => (
                    <div key={key}>
                      <dt className="text-xs font-medium text-gray-500 capitalize">
                        {key.replace(/([A-Z])/g, ' $1').trim()}
                      </dt>
                      <dd className="text-sm text-gray-900">{value}</dd>
                    </div>
                  ))}
                </dl>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

