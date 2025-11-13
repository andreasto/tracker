# Progress Tracking - Quick Reference

## Installation
```bash
npm install recharts
```

## Import Components
```typescript
import ProgressChart from './components/ProgressChart'
import CheckInForm from './components/CheckInForm'
import { api } from './services/api'
```

## Basic Usage

### Display Progress Chart
```typescript
<ProgressChart
  checkIns={checkInHistory}
  startWeight={initialWeight}
  startMeasurements={initialMeasurements}
/>
```

### Display Check-In Form
```typescript
<CheckInForm
  onSubmit={handleSubmit}
  onCancel={handleCancel}
/>
```

## API Calls

### Fetch Check-Ins
```typescript
const checkIns = await api.getCheckIns(userId)
```

### Submit Check-In
```typescript
await api.submitCheckIn(userId, {
  weight: 75.5,
  thigh: 55.0,
  glutes: 98.0,
  hips: 95.0,
  waist: 80.0,
  stomach: 85.0,
  chest: 100.0,
  overarm: 35.0
})
```

### Set Check-In Day
```typescript
await api.setCheckInDay(userId, 0) // 0 = Sunday, 6 = Saturday
```

## Data Structure

### CheckInState
```typescript
{
  userId: string
  weeklyCheckInDay?: number  // 0-6 (Sunday-Saturday)
  checkInHistory?: WeeklyCheckIn[]
}
```

### WeeklyCheckIn
```typescript
{
  submittedAt: string  // ISO date string
  measurements: {
    weight: number      // kg
    thigh: number       // cm
    glutes: number      // cm
    hips: number        // cm
    waist: number       // cm
    stomach: number     // cm
    chest: number       // cm
    overarm: number     // cm
  }
}
```

## Backend Endpoints

```
GET    /checkin/{userId}                  - Get check-ins
POST   /checkin/{userId}                  - Submit check-in
PUT    /checkin/{userId}/checkin-day      - Set check-in day
```

## Testing

### Manual Test
1. Start backend: `cd src/tracker.App && dotnet run`
2. Start frontend: `cd tracker-frontend && npm run dev`
3. Navigate to dashboard
4. Click "New Check-In" button
5. Fill form and submit

### Using test script
```bash
./test-checkin.sh
```

## Customization

### Change Chart Colors
Edit `measurementColors` in `ProgressChart.tsx`:
```typescript
const measurementColors: Record<MeasurementKey, string> = {
  weight: '#3b82f6',  // Your custom color
  // ...
}
```

### Modify Measurement Labels
Edit `measurementLabels` in `ProgressChart.tsx`:
```typescript
const measurementLabels: Record<MeasurementKey, string> = {
  weight: 'Your Custom Label',
  // ...
}
```

### Adjust Chart Height
In `ProgressChart.tsx`:
```typescript
<ResponsiveContainer width="100%" height={400}> {/* Change height */}
```

## Troubleshooting

### Chart not displaying
- Check if `checkInHistory` has data
- Verify API endpoint is returning correct format
- Check browser console for errors

### Data not updating after submit
- Ensure you're refreshing the check-in data after submit
- Verify API is returning 200 OK
- Check network tab in browser dev tools

### Type errors
- Ensure all required fields are provided in SubmitCheckInRequest
- Verify API response matches CheckInState interface

