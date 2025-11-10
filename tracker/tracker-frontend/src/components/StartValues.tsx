import { useState, FormEvent } from 'react'
import { api } from '../services/api'

interface StartValuesProps {
  userId: string
  onComplete: () => void
}

const measurementFields = [
  { id: 'chest', label: 'Chest (cm)', placeholder: '95' },
  { id: 'waist', label: 'Waist (cm)', placeholder: '80' },
  { id: 'hips', label: 'Hips (cm)', placeholder: '100' },
  { id: 'thighs', label: 'Thighs (cm)', placeholder: '60' },
  { id: 'arms', label: 'Arms (cm)', placeholder: '35' },
]

export default function StartValues({ userId, onComplete }: StartValuesProps) {
  const [startWeight, setStartWeight] = useState('')
  const [measurements, setMeasurements] = useState<Record<string, string>>({})
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleMeasurementChange = (id: string, value: string) => {
    setMeasurements(prev => ({ ...prev, [id]: value }))
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    setLoading(true)

    try {
      // Convert string measurements to numbers
      const numericMeasurements: Record<string, number> = {}
      Object.entries(measurements).forEach(([key, value]) => {
        if (value.trim()) {
          numericMeasurements[key] = parseFloat(value)
        }
      })

      await api.provideStartValues(userId, {
        startWeight: parseFloat(startWeight),
        measurements: numericMeasurements,
      })
      onComplete()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to submit measurements')
    } finally {
      setLoading(false)
    }
  }

  const isFormValid = startWeight.trim() !== '' && Object.keys(measurements).length > 0

  return (
    <div className="bg-white rounded-lg shadow-md p-8">
      <div className="text-center mb-8">
        <h2 className="text-3xl font-bold text-gray-900">Starting Measurements</h2>
        <p className="mt-2 text-sm text-gray-600">
          Track your progress from day one
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <div>
          <label htmlFor="startWeight" className="block text-sm font-medium text-gray-700 mb-2">
            Starting Weight (kg) *
          </label>
          <input
            id="startWeight"
            type="number"
            step="0.1"
            value={startWeight}
            onChange={(e) => setStartWeight(e.target.value)}
            placeholder="75.5"
            className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            required
          />
        </div>

        <div className="border-t border-gray-200 pt-6">
          <h3 className="text-lg font-medium text-gray-900 mb-4">
            Body Measurements (optional)
          </h3>
          <div className="space-y-4">
            {measurementFields.map((field) => (
              <div key={field.id}>
                <label htmlFor={field.id} className="block text-sm font-medium text-gray-700 mb-2">
                  {field.label}
                </label>
                <input
                  id={field.id}
                  type="number"
                  step="0.1"
                  value={measurements[field.id] || ''}
                  onChange={(e) => handleMeasurementChange(field.id, e.target.value)}
                  placeholder={field.placeholder}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
            ))}
          </div>
        </div>

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

