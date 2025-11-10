import { useState } from 'react'
import { Routes, Route, useNavigate } from 'react-router-dom'
import UserRegistration from './UserRegistration'
import Questionnaire from './Questionnaire'
import StartValues from './StartValues'
import CompletionScreen from './CompletionScreen'

export default function RegistrationFlow() {
  const [userId, setUserId] = useState<string>('')
  const navigate = useNavigate()

  const handleUserCreated = (id: string) => {
    setUserId(id)
    navigate('/register/questionnaire')
  }

  const handleQuestionnaireComplete = () => {
    navigate('/register/measurements')
  }

  const handleMeasurementsComplete = () => {
    navigate('/register/complete')
  }

  const handleOnboardingComplete = () => {
    navigate(`/dashboard/${userId}`)
  }

  return (
    <div className="min-h-screen flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <Routes>
          <Route
            path="/"
            element={<UserRegistration onUserCreated={handleUserCreated} />}
          />
          <Route
            path="/questionnaire"
            element={
              <Questionnaire
                userId={userId}
                onComplete={handleQuestionnaireComplete}
              />
            }
          />
          <Route
            path="/measurements"
            element={
              <StartValues
                userId={userId}
                onComplete={handleMeasurementsComplete}
              />
            }
          />
          <Route
            path="/complete"
            element={
              <CompletionScreen
                userId={userId}
                onComplete={handleOnboardingComplete}
              />
            }
          />
        </Routes>
      </div>
    </div>
  )
}

