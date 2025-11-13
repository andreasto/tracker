import { useMemo, useState } from 'react'
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts'
import { WeeklyCheckIn } from '../services/api'

interface ProgressChartProps {
  checkIns: WeeklyCheckIn[]
  startWeight?: number
  startMeasurements?: Record<string, number>
}

type MeasurementKey = 'weight' | 'thigh' | 'glutes' | 'hips' | 'waist' | 'stomach' | 'chest' | 'overarm'

const measurementColors: Record<MeasurementKey, string> = {
  weight: '#3b82f6', // blue
  chest: '#ef4444', // red
  waist: '#10b981', // green
  hips: '#f59e0b', // amber
  glutes: '#8b5cf6', // purple
  thigh: '#ec4899', // pink
  stomach: '#14b8a6', // teal
  overarm: '#f97316', // orange
}

const measurementLabels: Record<MeasurementKey, string> = {
  weight: 'Weight (kg)',
  chest: 'Chest (cm)',
  waist: 'Waist (cm)',
  hips: 'Hips (cm)',
  glutes: 'Glutes (cm)',
  thigh: 'Thigh (cm)',
  stomach: 'Stomach (cm)',
  overarm: 'Overarm (cm)',
}

export default function ProgressChart({ checkIns, startWeight, startMeasurements }: ProgressChartProps) {
  const [selectedMeasurements, setSelectedMeasurements] = useState<Set<MeasurementKey>>(
    new Set(['weight'])
  )

  const chartData = useMemo(() => {
    const data = checkIns.map((checkIn, index) => {
      const date = new Date(checkIn.submittedAt)
      return {
        name: date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
        date: date.toISOString(),
        week: `Week ${index + 1}`,
        weight: checkIn.measurements.weight,
        chest: checkIn.measurements.chest,
        waist: checkIn.measurements.waist,
        hips: checkIn.measurements.hips,
        glutes: checkIn.measurements.glutes,
        thigh: checkIn.measurements.thigh,
        stomach: checkIn.measurements.stomach,
        overarm: checkIn.measurements.overarm,
      }
    })

    // Add initial data point with start weight and measurements if available
    if (startWeight) {
      const initialPoint = {
        name: 'Start',
        date: new Date().toISOString(),
        week: 'Start',
        weight: startWeight,
        chest: startMeasurements?.chest || 0,
        waist: startMeasurements?.waist || 0,
        hips: startMeasurements?.hips || 0,
        glutes: startMeasurements?.glutes || 0,
        thigh: startMeasurements?.thigh || 0,
        stomach: startMeasurements?.stomach || 0,
        overarm: startMeasurements?.overarm || 0,
      }
      data.unshift(initialPoint)
    }

    return data
  }, [checkIns, startWeight, startMeasurements])

  const toggleMeasurement = (measurement: MeasurementKey) => {
    setSelectedMeasurements((prev) => {
      const newSet = new Set(prev)
      if (newSet.has(measurement)) {
        if (newSet.size > 1) {
          newSet.delete(measurement)
        }
      } else {
        newSet.add(measurement)
      }
      return newSet
    })
  }

  const getYAxisDomain = useMemo(() => {
    if (chartData.length === 0) return ['auto', 'auto']

    const values: number[] = []
    chartData.forEach((data) => {
      selectedMeasurements.forEach((measurement) => {
        const value = data[measurement]
        if (value > 0) {
          values.push(value)
        }
      })
    })

    if (values.length === 0) return ['auto', 'auto']

    const min = Math.min(...values)
    const max = Math.max(...values)
    const padding = (max - min) * 0.1

    return [Math.floor(min - padding), Math.ceil(max + padding)]
  }, [chartData, selectedMeasurements])

  if (chartData.length === 0) {
    return (
      <div className="bg-gray-50 rounded-lg p-8 text-center">
        <p className="text-gray-500">No check-in data yet. Start tracking your progress!</p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {/* Measurement toggles */}
      <div className="flex flex-wrap gap-2">
        {(Object.keys(measurementLabels) as MeasurementKey[]).map((measurement) => (
          <button
            key={measurement}
            onClick={() => toggleMeasurement(measurement)}
            className={`px-3 py-1.5 rounded-full text-sm font-medium transition-colors ${
              selectedMeasurements.has(measurement)
                ? 'text-white shadow-sm'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
            style={{
              backgroundColor: selectedMeasurements.has(measurement)
                ? measurementColors[measurement]
                : undefined,
            }}
          >
            {measurementLabels[measurement]}
          </button>
        ))}
      </div>

      {/* Chart */}
      <div className="bg-white rounded-lg p-4 shadow">
        <ResponsiveContainer width="100%" height={400}>
          <LineChart
            data={chartData}
            margin={{ top: 5, right: 30, left: 20, bottom: 5 }}
          >
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="name" />
            <YAxis domain={getYAxisDomain} />
            <Tooltip
              contentStyle={{
                backgroundColor: 'rgba(255, 255, 255, 0.95)',
                border: '1px solid #e5e7eb',
                borderRadius: '0.5rem',
              }}
            />
            <Legend />
            {(Array.from(selectedMeasurements) as MeasurementKey[]).map((measurement) => (
              <Line
                key={measurement}
                type="monotone"
                dataKey={measurement}
                stroke={measurementColors[measurement]}
                strokeWidth={2}
                dot={{ fill: measurementColors[measurement], r: 4 }}
                activeDot={{ r: 6 }}
                name={measurementLabels[measurement]}
              />
            ))}
          </LineChart>
        </ResponsiveContainer>
      </div>

      {/* Stats summary */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {(Array.from(selectedMeasurements) as MeasurementKey[]).map((measurement) => {
          const values = chartData
            .map((d) => d[measurement])
            .filter((v) => v > 0)
          
          if (values.length === 0) return null

          const current = values[values.length - 1]
          const initial = values[0]
          const change = current - initial
          const percentChange = ((change / initial) * 100).toFixed(1)

          return (
            <div
              key={measurement}
              className="bg-gray-50 rounded-lg p-4 border-l-4"
              style={{ borderColor: measurementColors[measurement] }}
            >
              <div className="text-sm font-medium text-gray-600 mb-1">
                {measurementLabels[measurement]}
              </div>
              <div className="text-2xl font-bold text-gray-900">
                {current.toFixed(1)}
              </div>
              <div
                className={`text-sm font-medium mt-1 ${
                  change < 0 ? 'text-green-600' : change > 0 ? 'text-red-600' : 'text-gray-600'
                }`}
              >
                {change > 0 ? '+' : ''}
                {change.toFixed(1)} ({change > 0 ? '+' : ''}
                {percentChange}%)
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}

