import { ReactNode, useEffect, useState } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { tokenManager, api } from '../services/api'

interface ProtectedRouteProps {
  children: ReactNode
}

export default function ProtectedRoute({ children }: ProtectedRouteProps) {
  const location = useLocation()
  const [isChecking, setIsChecking] = useState(true)
  const [isAuthenticated, setIsAuthenticated] = useState(false)

  useEffect(() => {
    const checkAuth = async () => {
      // First, check if we have a token
      if (!tokenManager.isAuthenticated()) {
        setIsAuthenticated(false)
        setIsChecking(false)
        return
      }

      // Try to verify the token is valid by fetching current user
      try {
        await api.getCurrentUser()
        setIsAuthenticated(true)
      } catch (error) {
        // Token is invalid or expired, and refresh failed
        tokenManager.removeTokens()
        setIsAuthenticated(false)
      } finally {
        setIsChecking(false)
      }
    }

    checkAuth()
  }, [])

  if (isChecking) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Verifying authentication...</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    // Redirect to login, but save the location they were trying to access
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  return <>{children}</>
}

