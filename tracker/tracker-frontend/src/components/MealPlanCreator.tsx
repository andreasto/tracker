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
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [showEndDate, setShowEndDate] = useState(false)

  useEffect(() => {
    const fetchData = async () => {
      try {
        const userData = await api.getCurrentUser()
        setUser(userData)
        
        // Fetch existing meal plans
        const plans = await api.getUserMealPlans(userData.userId)
        setExistingPlans(plans)
        
        // Set default start date to today
        const today = new Date().toISOString().split('T')[0]
        setStartDate(today)
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
    
    if (!startDate) {
      setError('Please select a start date')
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
        startDate: startDate,
        endDate: showEndDate && endDate ? endDate : undefined,
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
                    <p className="text-sm text-gray-600">Meals Per Day</p>
                    <p className="text-2xl font-bold text-green-600">{mealsPerDay} meals</p>
                  </div>
                </div>
                {dailyCalorieTarget && (
                  <p className="mt-3 text-xs text-gray-600">
                    💡 Your meal plan will be distributed across {mealsPerDay} meals, 
                    approximately {Math.round(dailyCalorieTarget / parseInt(mealsPerDay))} kcal per meal
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

                  <div>
                    <label htmlFor="startDate" className="block text-sm font-medium text-gray-700">
                      Start Date *
                    </label>
                    <input
                      type="date"
                      id="startDate"
                      value={startDate}
                      onChange={(e) => setStartDate(e.target.value)}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-4 py-2 border"
                      required
                    />
                  </div>

                  <div>
                    <div className="flex items-center mb-2">
                      <input
                        type="checkbox"
                        id="hasEndDate"
                        checked={showEndDate}
                        onChange={(e) => setShowEndDate(e.target.checked)}
                        className="rounded border-gray-300 text-blue-600 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                      />
                      <label htmlFor="hasEndDate" className="ml-2 block text-sm text-gray-700">
                        Set an end date (optional)
                      </label>
                    </div>
                    
                    {showEndDate && (
                      <input
                        type="date"
                        id="endDate"
                        value={endDate}
                        onChange={(e) => setEndDate(e.target.value)}
                        min={startDate}
                        className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-4 py-2 border"
                      />
                    )}
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
                  <li>A meal plan will be created with meals distributed across {mealsPerDay} meals per day</li>
                  <li>Each day will target approximately {dailyCalorieTarget || '...'} kcal based on your BMR and activity level</li>
                  <li>You can then select and customize recipes for each meal</li>
                  <li>The system will help balance your macros (protein, carbs, fat)</li>
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
                      className="border border-gray-200 rounded-lg p-4 hover:border-blue-400 hover:shadow-md transition-all cursor-pointer"
                      onClick={() => handleViewPlan(plan.mealPlanId)}
                    >
                      <h3 className="font-semibold text-gray-900 mb-1">{plan.planName}</h3>
                      <p className="text-xs text-gray-500">
                        Start: {new Date(plan.startDate).toLocaleDateString()}
                      </p>
                      {plan.endDate && (
                        <p className="text-xs text-gray-500">
                          End: {new Date(plan.endDate).toLocaleDateString()}
                        </p>
                      )}
                      {plan.totalKcal && (
                        <div className="mt-2 pt-2 border-t border-gray-100">
                          <p className="text-xs font-medium text-blue-600">
                            {Math.round(plan.totalKcal)} kcal/day
                          </p>
                        </div>
                      )}
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

