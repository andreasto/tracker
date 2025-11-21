import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { api, User, CheckInState, SubmitCheckInRequest, UserOnboardingState } from '../services/api'
import ProgressChart from './ProgressChart'
import CheckInForm from './CheckInForm'
import CheckInList from './CheckInList'

export default function Dashboard() {
  const { userId } = useParams<{ userId: string }>()
  const navigate = useNavigate()
  const [user, setUser] = useState<User | null>(null)
  const [checkIns, setCheckIns] = useState<CheckInState | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showCheckInForm, setShowCheckInForm] = useState(false)

  useEffect(() => {
    const fetchData = async () => {
      try {
        // First get current user (uses /me endpoint which is authenticated)
        const userData = await api.getCurrentUser()
        setUser(userData)
        
        // Then get check-ins for that user
        const checkInData = await api.getCheckIns(userData.userId)
        setCheckIns(checkInData)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load data')
        // If authentication fails, redirect to login
        if (err instanceof Error && err.message.includes('Authentication required')) {
          navigate('/login')
        }
      } finally {
        setLoading(false)
      }
    }

    fetchData()
  }, [navigate])

  const handleSubmitCheckIn = async (checkInData: SubmitCheckInRequest) => {
    if (!user) return

    await api.submitCheckIn(user.userId, checkInData)
    
    // Refresh check-in data
    const updatedCheckIns = await api.getCheckIns(user.userId)
    setCheckIns(updatedCheckIns)
    setShowCheckInForm(false)
  }

  const handleLogout = async () => {
    try {
      await api.logout()
      navigate('/login')
    } catch (error) {
      console.error('Logout error:', error)
      // Still redirect to login even if logout API call fails
      navigate('/login')
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading...</p>
        </div>
      </div>
    )
  }

  if (error || !user) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="bg-red-50 border border-red-200 text-red-700 px-6 py-4 rounded-lg">
          {error || 'User not found'}
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-7xl mx-auto">
        <div className="bg-white shadow rounded-lg p-6 mb-6">
          <div className="border-b border-gray-200 pb-4 mb-6 flex justify-between items-center">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
              <p className="mt-1 text-sm text-gray-500">Welcome back, {user.name}!</p>
            </div>
            <div className="flex gap-3">
              <button
                onClick={() => navigate('/meal-plan/create')}
                className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 font-medium"
              >
                🍽️ Create Meal Plan
              </button>
              <button
                onClick={() => setShowCheckInForm(true)}
                className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 font-medium"
              >
                📊 New Check-In
              </button>
              <button
                onClick={handleLogout}
                className="px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700 font-medium"
              >
                🚪 Logout
              </button>
            </div>
          </div>

          {/* Progress Chart */}
          {checkIns && (
            <div className="mb-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">Progress Tracking</h2>
              <ProgressChart
                checkIns={checkIns.checkInHistory || []}
                startWeight={user.startWeight}
                startMeasurements={user.measurements}
              />
            </div>
          )}

          {/* Check-In History List */}
          {checkIns && (
            <div className="mb-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">Check-In History</h2>
              <CheckInList
                checkIns={checkIns.checkInHistory || []}
                startWeight={user.startWeight}
                startMeasurements={user.measurements}
              />
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* User Profile */}
            <div className="bg-gray-50 p-4 rounded-lg">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Profile</h2>
              <dl className="space-y-2">
                <div>
                  <dt className="text-sm font-medium text-gray-500">User ID</dt>
                  <dd className="text-sm text-gray-900">{user.userId}</dd>
                </div>
                <div>
                  <dt className="text-sm font-medium text-gray-500">Name</dt>
                  <dd className="text-sm text-gray-900">{user.name}</dd>
                </div>
                <div>
                  <dt className="text-sm font-medium text-gray-500">Email</dt>
                  <dd className="text-sm text-gray-900">{user.email}</dd>
                </div>
                <div>
                  <dt className="text-sm font-medium text-gray-500">Status</dt>
                  <dd className="text-sm">
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                      {user.onboardingState === UserOnboardingState.Complete ? '✅ Onboarding Completed' : '🚀 Onboarding In Progress'}
                    </span>
                  </dd>
                </div>
              </dl>
            </div>

            {/* Questionnaire Answers */}
            {user.questionnaireAnswers && (
              <div className="bg-gray-50 p-4 rounded-lg">
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Your Goals</h2>
                <dl className="space-y-2">
                  {Object.entries(user.questionnaireAnswers).map(([key, value]) => (
                    <div key={key}>
                      <dt className="text-sm font-medium text-gray-500 capitalize">
                        {key.replace(/([A-Z])/g, ' $1').trim()}
                      </dt>
                      <dd className="text-sm text-gray-900">{value}</dd>
                    </div>
                  ))}
                </dl>
              </div>
            )}

            {/* Start Values */}
            {user.startWeight && (
              <div className="bg-gray-50 p-4 rounded-lg">
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Starting Stats</h2>
                <dl className="space-y-2">
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Weight</dt>
                    <dd className="text-sm text-gray-900">{user.startWeight} kg</dd>
                  </div>
                </dl>
              </div>
            )}

            {/* Measurements */}
            {user.measurements && Object.keys(user.measurements).length > 0 && (
              <div className="bg-gray-50 p-4 rounded-lg">
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Measurements</h2>
                <dl className="space-y-2">
                  {Object.entries(user.measurements).map(([key, value]) => (
                    <div key={key}>
                      <dt className="text-sm font-medium text-gray-500 capitalize">{key}</dt>
                      <dd className="text-sm text-gray-900">{value} cm</dd>
                    </div>
                  ))}
                </dl>
              </div>
            )}

            {/* BMR Information */}
            {user.bmrBase && user.bmrWithActivityLevel && (
              <div className="bg-blue-50 p-4 rounded-lg border-2 border-blue-200">
                <h2 className="text-lg font-semibold text-gray-900 mb-4">
                  💪 Your Calorie Needs
                </h2>
                <dl className="space-y-3">
                  <div>
                    <dt className="text-sm font-medium text-gray-600">Base Metabolic Rate (BMR)</dt>
                    <dd className="text-2xl font-bold text-blue-600">
                      {Math.round(user.bmrBase)} <span className="text-sm font-normal text-gray-600">kcal/day</span>
                    </dd>
                    <dd className="text-xs text-gray-500 mt-1">
                      Calories burned at rest
                    </dd>
                  </div>
                  <div className="pt-3 border-t border-blue-200">
                    <dt className="text-sm font-medium text-gray-600">Daily Calorie Target</dt>
                    <dd className="text-2xl font-bold text-green-600">
                      {Math.round(user.bmrWithActivityLevel)} <span className="text-sm font-normal text-gray-600">kcal/day</span>
                    </dd>
                    <dd className="text-xs text-gray-500 mt-1">
                      Including activity level
                    </dd>
                  </div>
                </dl>
              </div>
            )}
          </div>

          <div className="mt-8 pt-6 border-t border-gray-200">
            <p className="text-center text-gray-500 text-sm">
              Your tracking journey starts here! 🎯
            </p>
          </div>
        </div>

        {/* Check-In Form Modal */}
        {showCheckInForm && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
            <div className="max-w-2xl w-full max-h-[90vh] overflow-y-auto">
              <CheckInForm
                onSubmit={handleSubmitCheckIn}
                onCancel={() => setShowCheckInForm(false)}
              />
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

