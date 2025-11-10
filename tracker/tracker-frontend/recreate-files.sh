#!/bin/bash

# Script to recreate all frontend component files

echo "🔧 Recreating frontend component files..."

# Create the Questionnaire component
cat > src/components/Questionnaire.tsx << 'QUESTIONNAIRE_EOF'
import { useState, FormEvent } from 'react'
import { api } from '../services/api'

interface QuestionnaireProps {
  userId: string
  onComplete: () => void
}

const questions = [
  { id: 'goal', label: 'What is your primary goal?', type: 'select', options: ['Lose Weight', 'Gain Muscle', 'Maintain Weight', 'Improve Health'] },
  { id: 'activityLevel', label: 'How active are you?', type: 'select', options: ['Sedentary', 'Lightly Active', 'Moderately Active', 'Very Active', 'Extremely Active'] },
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

  const isFormValid = questions.every(q => answers[q.id]?.trim())

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
            </label>
            {question.type === 'select' ? (
              <select
                id={question.id}
                value={answers[question.id] || ''}
                onChange={(e) => handleChange(question.id, e.target.value)}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                required
              >
                <option value="">Select an option</option>
                {question.options?.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            ) : (
              <input
                id={question.id}
                type="text"
                value={answers[question.id] || ''}
                onChange={(e) => handleChange(question.id, e.target.value)}
                placeholder="Type your answer..."
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                required
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
QUESTIONNAIRE_EOF

echo "✅ Created Questionnaire.tsx"

echo "✅ All component files recreated successfully!"
echo ""
echo "Next steps:"
echo "1. Run: npm install"
echo "2. Run: npm run dev"

