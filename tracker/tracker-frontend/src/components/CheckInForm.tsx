import { useState } from 'react'
import { SubmitCheckInRequest } from '../services/api'

interface CheckInFormProps {
  onSubmit: (checkIn: SubmitCheckInRequest) => Promise<void>
  onCancel: () => void
}

export default function CheckInForm({ onSubmit, onCancel }: CheckInFormProps) {
  const [formData, setFormData] = useState<SubmitCheckInRequest>({
    weight: 0,
    thigh: 0,
    glutes: 0,
    hips: 0,
    waist: 0,
    stomach: 0,
    chest: 0,
    overarm: 0,
  })
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleChange = (field: keyof SubmitCheckInRequest, value: string) => {
    const numValue = parseFloat(value) || 0
    setFormData((prev) => ({ ...prev, [field]: numValue }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      await onSubmit(formData)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit check-in')
    } finally {
      setIsSubmitting(false)
    }
  }

  const measurements: Array<{
    key: keyof SubmitCheckInRequest
    label: string
    icon: string
  }> = [
    { key: 'weight', label: 'Weight (kg)', icon: '⚖️' },
    { key: 'chest', label: 'Chest (cm)', icon: '💪' },
    { key: 'waist', label: 'Waist (cm)', icon: '🎯' },
    { key: 'stomach', label: 'Stomach (cm)', icon: '🎯' },
    { key: 'hips', label: 'Hips (cm)', icon: '📏' },
    { key: 'glutes', label: 'Glutes (cm)', icon: '📏' },
    { key: 'thigh', label: 'Thigh (cm)', icon: '📏' },
    { key: 'overarm', label: 'Overarm (cm)', icon: '💪' },
  ]

  return (
    <div className="bg-white rounded-lg shadow-lg p-6">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Weekly Check-In</h2>
        <button
          onClick={onCancel}
          className="text-gray-400 hover:text-gray-600"
          type="button"
        >
          <svg
            className="w-6 h-6"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M6 18L18 6M6 6l12 12"
            />
          </svg>
        </button>
      </div>

      {error && (
        <div className="mb-4 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
          {measurements.map(({ key, label, icon }) => (
            <div key={key}>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                {icon} {label}
              </label>
              <input
                type="number"
                step="0.1"
                min="0"
                value={formData[key] || ''}
                onChange={(e) => handleChange(key, e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>
          ))}
        </div>

        <div className="flex justify-end gap-3">
          <button
            type="button"
            onClick={onCancel}
            className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50"
            disabled={isSubmitting}
          >
            Cancel
          </button>
          <button
            type="submit"
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Submitting...' : 'Submit Check-In'}
          </button>
        </div>
      </form>
    </div>
  )
}

