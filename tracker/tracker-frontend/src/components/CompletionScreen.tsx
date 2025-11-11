import { useState, useEffect } from 'react'
import { api, User } from '../services/api'

interface CompletionScreenProps {
  userId: string
  onComplete: () => void
}

export default function CompletionScreen({ userId, onComplete }: CompletionScreenProps) {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [user, setUser] = useState<User | null>(null)

  useEffect(() => {
    const fetchUser = async () => {
      try {
        const userData = await api.getUser(userId)
        setUser(userData)
      } catch (err) {
        console.error('Failed to fetch user data:', err)
      }
    }
    fetchUser()
  }, [userId])

  const handleComplete = async () => {
    setError(null)
    setLoading(true)

    try {
      await api.completeOnboarding(userId)
      // Fetch updated user data with BMR
      const updatedUser = await api.getUser(userId)
      setUser(updatedUser)
      onComplete()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to complete onboarding')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="bg-white rounded-lg shadow-md p-8">
      <div className="text-center">
        <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-green-100 mb-6">
          <svg
            className="h-10 w-10 text-green-600"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M5 13l4 4L19 7"
            />
          </svg>
        </div>
        
        <h2 className="text-3xl font-bold text-gray-900 mb-4">
          You're All Set!
        </h2>
        
        <p className="text-lg text-gray-600 mb-8">
          Your profile is complete and you're ready to start tracking your progress.
        </p>

        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-8">
          <p className="text-sm text-blue-800">
            <strong>User ID:</strong> {userId}
          </p>
          <p className="text-xs text-blue-600 mt-2">
            Save this ID - you'll need it to access your account
          </p>
        </div>

        {user?.bmrBase && user?.bmrWithActivityLevel && (
          <div className="bg-gradient-to-r from-green-50 to-blue-50 border-2 border-green-200 rounded-lg p-6 mb-8">
            <h3 className="text-lg font-semibold text-gray-900 mb-4 text-center">
              🎯 Your Personalized Calorie Targets
            </h3>
            <div className="grid grid-cols-2 gap-4">
              <div className="text-center">
                <p className="text-sm text-gray-600 mb-1">Base BMR</p>
                <p className="text-2xl font-bold text-blue-600">
                  {Math.round(user.bmrBase)}
                </p>
                <p className="text-xs text-gray-500">kcal/day</p>
              </div>
              <div className="text-center">
                <p className="text-sm text-gray-600 mb-1">Daily Target</p>
                <p className="text-2xl font-bold text-green-600">
                  {Math.round(user.bmrWithActivityLevel)}
                </p>
                <p className="text-xs text-gray-500">kcal/day</p>
              </div>
            </div>
          </div>
        )}

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">
            {error}
          </div>
        )}

        <button
          onClick={handleComplete}
          disabled={loading}
          className="w-full flex justify-center py-3 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? 'Completing...' : 'Go to Dashboard'}
        </button>
      </div>
    </div>
  )
}

