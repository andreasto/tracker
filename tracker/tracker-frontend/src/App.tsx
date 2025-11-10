import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import RegistrationFlow from './components/RegistrationFlow'
import Dashboard from './components/Dashboard'

function App() {
  return (
    <Router>
      <div className="min-h-screen bg-gray-50">
        <Routes>
          <Route path="/" element={<Navigate to="/register" replace />} />
          <Route path="/register/*" element={<RegistrationFlow />} />
          <Route path="/dashboard/:userId" element={<Dashboard />} />
        </Routes>
      </div>
    </Router>
  )
}

export default App
