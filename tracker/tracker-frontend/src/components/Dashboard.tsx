import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { api, User } from '../services/api'

export default function Dashboard() {
  const { userId } = useParams<{ userId: string }>()
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchUser = async () => {
      if (!userId) return

      try {
        const userData = await api.getUser(userId)
        setUser(userData)
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load user data')
      } finally {
        setLoading(false)
      }
    }

    fetchUser()
  }, [userId])

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
      <div className="max-w-4xl mx-auto">
        <div className="bg-white shadow rounded-lg p-6">
          <div className="border-b border-gray-200 pb-4 mb-6">
            <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
            <p className="mt-1 text-sm text-gray-500">Welcome back, {user.name}!</p>
          </div>

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
                      {user.onboardingState}
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
      </div>
    </div>
  )
}

