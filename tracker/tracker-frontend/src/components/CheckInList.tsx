import { WeeklyCheckIn } from '../services/api'

interface CheckInListProps {
  checkIns: WeeklyCheckIn[]
  startWeight?: number
  startMeasurements?: Record<string, number>
}

interface CheckInEntry {
  isInitial: boolean
  submittedAt: string | null
  measurements: {
    weight: number
    chest: number
    waist: number
    stomach: number
    hips: number
    glutes: number
    thigh: number
    overarm: number
  }
}

export default function CheckInList({ checkIns, startWeight, startMeasurements }: CheckInListProps) {
  // Combine initial data with check-ins for display
  const allEntries: CheckInEntry[] = []

  // Add initial entry if available
  if (startWeight) {
    allEntries.push({
      isInitial: true,
      submittedAt: null,
      measurements: {
        weight: startWeight,
        chest: startMeasurements?.chest || 0,
        waist: startMeasurements?.waist || 0,
        stomach: startMeasurements?.stomach || 0,
        hips: startMeasurements?.hips || 0,
        glutes: startMeasurements?.glutes || 0,
        thigh: startMeasurements?.thigh || 0,
        overarm: startMeasurements?.overarm || 0,
      }
    })
  }

  // Add all check-ins
  checkIns.forEach(checkIn => {
    allEntries.push({
      isInitial: false,
      submittedAt: checkIn.submittedAt,
      measurements: checkIn.measurements
    })
  })

  // Reverse to show most recent first
  const sortedEntries = [...allEntries].reverse()

  const formatDate = (dateString: string | null) => {
    if (!dateString) return 'Initial Values'
    const date = new Date(dateString)
    return date.toLocaleDateString('en-US', { 
      year: 'numeric',
      month: 'short', 
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  const calculateChange = (current: number, initial: number) => {
    if (initial === 0) return null
    const change = current - initial
    const percent = ((change / initial) * 100).toFixed(1)
    return { change, percent }
  }

  if (allEntries.length === 0) {
    return (
      <div className="bg-gray-50 rounded-lg p-8 text-center">
        <p className="text-gray-500">No check-in data yet. Start tracking your progress!</p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {sortedEntries.map((entry, index) => {
        const isRecent = index === 0 && !entry.isInitial
        const initialEntry = allEntries[0] // First entry (initial or first check-in)
        
        return (
          <div 
            key={entry.submittedAt || 'initial'}
            className={`bg-white rounded-lg shadow border ${
              entry.isInitial 
                ? 'border-blue-200 bg-blue-50' 
                : isRecent 
                  ? 'border-green-200 bg-green-50' 
                  : 'border-gray-200'
            }`}
          >
            <div className="p-4">
              {/* Header */}
              <div className="flex justify-between items-center mb-4">
                <div>
                  <h3 className="text-lg font-semibold text-gray-900">
                    {entry.isInitial ? '📋 Initial Values' : `📊 Check-In #${allEntries.length - index - 1}`}
                  </h3>
                  <p className="text-sm text-gray-500">
                    {formatDate(entry.submittedAt)}
                  </p>
                </div>
                {isRecent && (
                  <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                    Latest
                  </span>
                )}
                {entry.isInitial && (
                  <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                    Starting Point
                  </span>
                )}
              </div>

              {/* Measurements Grid */}
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
                {/* Weight */}
                <div className="bg-white rounded-lg p-3 border border-gray-200">
                  <div className="text-xs font-medium text-gray-500 mb-1">⚖️ Weight</div>
                  <div className="text-xl font-bold text-gray-900">
                    {entry.measurements.weight.toFixed(1)} <span className="text-sm font-normal text-gray-500">kg</span>
                  </div>
                  {!entry.isInitial && initialEntry && (() => {
                    const change = calculateChange(entry.measurements.weight, initialEntry.measurements.weight)
                    return change && (
                      <div className={`text-xs font-medium mt-1 ${
                        change.change < 0 ? 'text-green-600' : change.change > 0 ? 'text-red-600' : 'text-gray-600'
                      }`}>
                        {change.change > 0 ? '+' : ''}{change.change.toFixed(1)} ({change.change > 0 ? '+' : ''}{change.percent}%)
                      </div>
                    )
                  })()}
                </div>

                {/* Chest */}
                {entry.measurements.chest > 0 && (
                  <div className="bg-white rounded-lg p-3 border border-gray-200">
                    <div className="text-xs font-medium text-gray-500 mb-1">💪 Chest</div>
                    <div className="text-xl font-bold text-gray-900">
                      {entry.measurements.chest.toFixed(1)} <span className="text-sm font-normal text-gray-500">cm</span>
                    </div>
                    {!entry.isInitial && initialEntry && initialEntry.measurements.chest > 0 && (() => {
                      const change = calculateChange(entry.measurements.chest, initialEntry.measurements.chest)
                      return change && (
                        <div className={`text-xs font-medium mt-1 ${
                          change.change < 0 ? 'text-green-600' : change.change > 0 ? 'text-red-600' : 'text-gray-600'
                        }`}>
                          {change.change > 0 ? '+' : ''}{change.change.toFixed(1)} ({change.change > 0 ? '+' : ''}{change.percent}%)
                        </div>
                      )
                    })()}
                  </div>
                )}

                {/* Waist */}
                {entry.measurements.waist > 0 && (
                  <div className="bg-white rounded-lg p-3 border border-gray-200">
                    <div className="text-xs font-medium text-gray-500 mb-1">🎯 Waist</div>
                    <div className="text-xl font-bold text-gray-900">
                      {entry.measurements.waist.toFixed(1)} <span className="text-sm font-normal text-gray-500">cm</span>
                    </div>
                    {!entry.isInitial && initialEntry && initialEntry.measurements.waist > 0 && (() => {
                      const change = calculateChange(entry.measurements.waist, initialEntry.measurements.waist)
                      return change && (
                        <div className={`text-xs font-medium mt-1 ${
                          change.change < 0 ? 'text-green-600' : change.change > 0 ? 'text-red-600' : 'text-gray-600'
                        }`}>
                          {change.change > 0 ? '+' : ''}{change.change.toFixed(1)} ({change.change > 0 ? '+' : ''}{change.percent}%)
                        </div>
                      )
                    })()}
                  </div>
                )}

                {/* Stomach */}
                {entry.measurements.stomach > 0 && (
                  <div className="bg-white rounded-lg p-3 border border-gray-200">
                    <div className="text-xs font-medium text-gray-500 mb-1">🎯 Stomach</div>
                    <div className="text-xl font-bold text-gray-900">
                      {entry.measurements.stomach.toFixed(1)} <span className="text-sm font-normal text-gray-500">cm</span>
                    </div>
                    {!entry.isInitial && initialEntry && initialEntry.measurements.stomach > 0 && (() => {
                      const change = calculateChange(entry.measurements.stomach, initialEntry.measurements.stomach)
                      return change && (
                        <div className={`text-xs font-medium mt-1 ${
                          change.change < 0 ? 'text-green-600' : change.change > 0 ? 'text-red-600' : 'text-gray-600'
                        }`}>
                          {change.change > 0 ? '+' : ''}{change.change.toFixed(1)} ({change.change > 0 ? '+' : ''}{change.percent}%)
                        </div>
                      )
                    })()}
                  </div>
                )}

                {/* Hips */}
                {entry.measurements.hips > 0 && (
                  <div className="bg-white rounded-lg p-3 border border-gray-200">
                    <div className="text-xs font-medium text-gray-500 mb-1">📏 Hips</div>
                    <div className="text-xl font-bold text-gray-900">
                      {entry.measurements.hips.toFixed(1)} <span className="text-sm font-normal text-gray-500">cm</span>
                    </div>
                    {!entry.isInitial && initialEntry && initialEntry.measurements.hips > 0 && (() => {
                      const change = calculateChange(entry.measurements.hips, initialEntry.measurements.hips)
                      return change && (
                        <div className={`text-xs font-medium mt-1 ${
                          change.change < 0 ? 'text-green-600' : change.change > 0 ? 'text-red-600' : 'text-gray-600'
                        }`}>
                          {change.change > 0 ? '+' : ''}{change.change.toFixed(1)} ({change.change > 0 ? '+' : ''}{change.percent}%)
                        </div>
                      )
                    })()}
                  </div>
                )}

                {/* Glutes */}
                {entry.measurements.glutes > 0 && (
                  <div className="bg-white rounded-lg p-3 border border-gray-200">
                    <div className="text-xs font-medium text-gray-500 mb-1">📏 Glutes</div>
                    <div className="text-xl font-bold text-gray-900">
                      {entry.measurements.glutes.toFixed(1)} <span className="text-sm font-normal text-gray-500">cm</span>
                    </div>
                    {!entry.isInitial && initialEntry && initialEntry.measurements.glutes > 0 && (() => {
                      const change = calculateChange(entry.measurements.glutes, initialEntry.measurements.glutes)
                      return change && (
                        <div className={`text-xs font-medium mt-1 ${
                          change.change < 0 ? 'text-green-600' : change.change > 0 ? 'text-red-600' : 'text-gray-600'
                        }`}>
                          {change.change > 0 ? '+' : ''}{change.change.toFixed(1)} ({change.change > 0 ? '+' : ''}{change.percent}%)
                        </div>
                      )
                    })()}
                  </div>
                )}

                {/* Thigh */}
                {entry.measurements.thigh > 0 && (
                  <div className="bg-white rounded-lg p-3 border border-gray-200">
                    <div className="text-xs font-medium text-gray-500 mb-1">📏 Thigh</div>
                    <div className="text-xl font-bold text-gray-900">
                      {entry.measurements.thigh.toFixed(1)} <span className="text-sm font-normal text-gray-500">cm</span>
                    </div>
                    {!entry.isInitial && initialEntry && initialEntry.measurements.thigh > 0 && (() => {
                      const change = calculateChange(entry.measurements.thigh, initialEntry.measurements.thigh)
                      return change && (
                        <div className={`text-xs font-medium mt-1 ${
                          change.change < 0 ? 'text-green-600' : change.change > 0 ? 'text-red-600' : 'text-gray-600'
                        }`}>
                          {change.change > 0 ? '+' : ''}{change.change.toFixed(1)} ({change.change > 0 ? '+' : ''}{change.percent}%)
                        </div>
                      )
                    })()}
                  </div>
                )}

                {/* Overarm */}
                {entry.measurements.overarm > 0 && (
                  <div className="bg-white rounded-lg p-3 border border-gray-200">
                    <div className="text-xs font-medium text-gray-500 mb-1">💪 Overarm</div>
                    <div className="text-xl font-bold text-gray-900">
                      {entry.measurements.overarm.toFixed(1)} <span className="text-sm font-normal text-gray-500">cm</span>
                    </div>
                    {!entry.isInitial && initialEntry && initialEntry.measurements.overarm > 0 && (() => {
                      const change = calculateChange(entry.measurements.overarm, initialEntry.measurements.overarm)
                      return change && (
                        <div className={`text-xs font-medium mt-1 ${
                          change.change < 0 ? 'text-green-600' : change.change > 0 ? 'text-red-600' : 'text-gray-600'
                        }`}>
                          {change.change > 0 ? '+' : ''}{change.change.toFixed(1)} ({change.change > 0 ? '+' : ''}{change.percent}%)
                        </div>
                      )
                    })()}
                  </div>
                )}
              </div>
            </div>
          </div>
        )
      })}
    </div>
  )
}

