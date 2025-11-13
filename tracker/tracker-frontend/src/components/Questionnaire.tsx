import { useState, FormEvent } from 'react'
import { api } from '../services/api'

interface QuestionnaireProps {
  userId: string
  onComplete: () => void
}

interface Question {
  id: string
  label: string
  type: 'select' | 'number' | 'text'
  options?: string[]
  placeholder?: string
  required?: boolean
}

const questions: Question[] = [
  { id: 'gender', label: 'What is your gender?', type: 'select', options: ['Male', 'Female'], required: true },
  { id: 'age', label: 'What is your age?', type: 'number', placeholder: 'Enter your age', required: true },
  { id: 'height', label: 'What is your height? (in cm)', type: 'number', placeholder: 'e.g., 180', required: true },
  { id: 'activityLevel', label: 'How active are you?', type: 'select', options: ['Sedentary', 'Lightly Active', 'Moderately Active', 'Very Active', 'Extra Active'], required: true },
  { id: 'mealsPerDay', label: 'How many meals do you prefer per day?', type: 'select', options: ['3', '4', '5'], required: true },
  { id: 'goal', label: 'What is your primary goal?', type: 'select', options: ['Lose Weight', 'Gain Muscle', 'Maintain Weight', 'Improve Health'] },
  { id: 'experience', label: 'How experienced are you with tracking?', type: 'select', options: ['Beginner', 'Intermediate', 'Advanced'] },
  { id: 'motivation', label: 'What motivates you most?', type: 'text' },
]

export default function Questionnaire({ userId, onComplete }: QuestionnaireProps) {
  const [answers, setAnswers] = useState<Record<string, string>>({})
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleChange = (questionId: string, value: string) => {
    setAnswers(prev => ({ ...prev, [questionId]: value }))
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    setLoading(true)

    try {
      await api.answerQuestionnaire(userId, { answers })
      onComplete()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit questionnaire')
    } finally {
      setLoading(false)
    }
  }

  const isFormValid = questions
    .filter(q => q.required)
    .every(q => answers[q.id]?.trim())

  return (
    <div className="bg-white rounded-lg shadow-md p-8">
      <div className="text-center mb-8">
        <h2 className="text-3xl font-bold text-gray-900">Tell Us About Yourself</h2>
        <p className="mt-2 text-sm text-gray-600">
          This helps us personalize your experience
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {questions.map((question) => (
          <div key={question.id}>
            <label htmlFor={question.id} className="block text-sm font-medium text-gray-700 mb-2">
              {question.label}
              {question.required && <span className="text-red-500 ml-1">*</span>}
            </label>
            {question.type === 'select' ? (
              <select
                id={question.id}
                value={answers[question.id] || ''}
                onChange={(e) => handleChange(question.id, e.target.value)}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                required={question.required}
              >
                <option value="">Select an option</option>
                {question.options?.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            ) : question.type === 'number' ? (
              <input
                id={question.id}
                type="number"
                value={answers[question.id] || ''}
                onChange={(e) => handleChange(question.id, e.target.value)}
                placeholder={question.placeholder || 'Enter a number'}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                required={question.required}
                min="1"
                step="1"
              />
            ) : (
              <input
                id={question.id}
                type="text"
                value={answers[question.id] || ''}
                onChange={(e) => handleChange(question.id, e.target.value)}
                placeholder={question.placeholder || 'Type your answer...'}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                required={question.required}
              />
            )}
          </div>
        ))}

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
            {error}
          </div>
        )}

        <button
          type="submit"
          disabled={loading || !isFormValid}
          className="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? 'Submitting...' : 'Continue'}
        </button>
      </form>
    </div>
  )
}

