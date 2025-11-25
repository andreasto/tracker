import { BrowserRouter as Router, Routes, Route, Navigate, useNavigate } from 'react-router-dom'
import RegistrationFlow from './components/RegistrationFlow'
import Dashboard from './components/Dashboard'
import Login from './components/Login'
import ProtectedRoute from './components/ProtectedRoute'
import MealPlanCreator from './components/MealPlanCreator'
import MealPlanDetails from './components/MealPlanDetails'
import RecipeList from './components/RecipeList'
import RecipeCreator from './components/RecipeCreator'
import RecipeDetail from './components/RecipeDetail'
import { tokenManager } from './services/api'

function LoginPage() {
  const navigate = useNavigate()

  const handleLoginSuccess = () => {
    navigate('/dashboard')
  }

  const handleSwitchToRegister = () => {
    navigate('/register')
  }

  // If already logged in, redirect to dashboard
  if (tokenManager.isAuthenticated()) {
    return <Navigate to="/dashboard" replace />
  }

  return (
    <div className="min-h-screen flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8 bg-gray-50">
      <div className="max-w-md w-full">
        <Login onLoginSuccess={handleLoginSuccess} onSwitchToRegister={handleSwitchToRegister} />
      </div>
    </div>
  )
}

function App() {
  return (
    <Router>
      <div className="min-h-screen bg-gray-50">
        <Routes>
          <Route path="/" element={<Navigate to="/login" replace />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register/*" element={<RegistrationFlow />} />
          <Route 
            path="/dashboard" 
            element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            } 
          />
          {/* Legacy route support - redirects to /dashboard */}
          <Route 
            path="/dashboard/:userId" 
            element={<Navigate to="/dashboard" replace />}
          />
          {/* Meal Plan Routes */}
          <Route 
            path="/meal-plan/create" 
            element={
              <ProtectedRoute>
                <MealPlanCreator />
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/meal-plan/:mealPlanId" 
            element={
              <ProtectedRoute>
                <MealPlanDetails />
              </ProtectedRoute>
            } 
          />
          {/* Recipe Routes */}
          <Route 
            path="/recipes" 
            element={
              <ProtectedRoute>
                <RecipeList />
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/recipe/create" 
            element={
              <ProtectedRoute>
                <RecipeCreator />
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/recipe/:recipeId" 
            element={
              <ProtectedRoute>
                <RecipeDetail />
              </ProtectedRoute>
            } 
          />
        </Routes>
      </div>
    </Router>
  )
}

export default App
