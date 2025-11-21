import { BrowserRouter as Router, Routes, Route, Navigate, useNavigate } from 'react-router-dom'
import RegistrationFlow from './components/RegistrationFlow'
import Dashboard from './components/Dashboard'
import Login from './components/Login'

function LoginPage() {
  const navigate = useNavigate()

  const handleLoginSuccess = (userId: string) => {
    navigate(`/dashboard/${userId}`)
  }

  const handleSwitchToRegister = () => {
    navigate('/register')
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
          <Route path="/dashboard/:userId" element={<Dashboard />} />
        </Routes>
      </div>
    </Router>
  )
}

export default App
