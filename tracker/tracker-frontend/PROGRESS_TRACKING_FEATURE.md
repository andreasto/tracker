# Progress Tracking Feature

## Overview
The progress tracking feature allows users to visualize their fitness journey through interactive charts displaying weight and body measurements over time from weekly check-ins.

## Components

### 1. ProgressChart Component
Located: `src/components/ProgressChart.tsx`

**Features:**
- Interactive line chart showing progress over time
- Toggle between different measurements (weight, chest, waist, hips, glutes, thigh, stomach, overarm)
- Color-coded lines for each measurement type
- Stats summary showing current value, change from start, and percentage change
- Responsive design with smooth animations

**Props:**
- `checkIns`: Array of weekly check-in data
- `startWeight`: Optional initial weight from onboarding (shown as first data point)
- `startMeasurements`: Optional initial measurements from onboarding (shown as first data point)

### 2. CheckInForm Component
Located: `src/components/CheckInForm.tsx`

**Features:**
- Form to submit weekly check-ins
- Input fields for:
  - Weight (kg)
  - Chest, Waist, Stomach, Hips, Glutes, Thigh, Overarm (all in cm)
- Form validation
- Error handling
- Modal display

### 3. Updated Dashboard Component
Located: `src/components/Dashboard.tsx`

**New Features:**
- "New Check-In" button to open check-in form
- Progress chart section displaying historical data
- Modal overlay for check-in form
- Automatic data refresh after submitting check-in

## API Integration

### New Endpoints Added to `src/services/api.ts`

#### Get Check-Ins
```typescript
api.getCheckIns(userId: string): Promise<CheckInState>
```
Fetches all check-in data for a user.

#### Submit Check-In
```typescript
api.submitCheckIn(userId: string, request: SubmitCheckInRequest): Promise<void>
```
Submits a new weekly check-in.

#### Set Check-In Day
```typescript
api.setCheckInDay(userId: string, checkInDay: number): Promise<void>
```
Sets the preferred weekly check-in day.

### Type Definitions

```typescript
interface Measurements {
  weight: number
  thigh: number
  glutes: number
  hips: number
  waist: number
  stomach: number
  chest: number
  overarm: number
}

interface WeeklyCheckIn {
  submittedAt: string
  measurements: Measurements
}

interface CheckInState {
  userId: string
  weeklyCheckInDay?: number
  checkInHistory?: WeeklyCheckIn[]
}
```

## Usage

### Viewing Progress
1. Navigate to the dashboard
2. The progress chart appears automatically if there are check-ins
3. Click on measurement buttons to toggle different metrics on/off
4. View stats summary below the chart

### Submitting a Check-In
1. Click the "📊 New Check-In" button
2. Fill in all measurement fields
3. Click "Submit Check-In"
4. The chart will automatically update with the new data

## Dependencies

### New Package
- **recharts** (^2.x): A composable charting library built on React components
  - Used for rendering interactive line charts
  - Provides responsive containers, tooltips, legends, and axes

## Backend API Endpoints

The frontend expects these endpoints to be available:

- `GET /checkin/{userId}` - Get user's check-in history
- `POST /checkin/{userId}` - Submit a new check-in
- `PUT /checkin/{userId}/checkin-day` - Set preferred check-in day

## Chart Features

### Measurement Types
Each measurement is color-coded:
- 🔵 Weight (Blue)
- 🔴 Chest (Red)
- 🟢 Waist (Green)
- 🟠 Hips (Amber)
- 🟣 Glutes (Purple)
- 🌸 Thigh (Pink)
- 🔷 Stomach (Teal)
- 🟧 Overarm (Orange)

### Interactive Features
- **Toggle Measurements**: Click any measurement button to show/hide it on the chart
- **Tooltips**: Hover over data points to see exact values
- **Responsive**: Automatically adjusts to screen size
- **Auto-scaling**: Y-axis automatically adjusts based on selected measurements

### Stats Cards
Below the chart, cards show for each selected measurement:
- Current value
- Change from initial value (with +/- indicator)
- Percentage change
- Color-coded based on measurement type

## Testing

To test the progress tracking feature:

1. Start the backend API
2. Start the frontend dev server: `npm run dev`
3. Complete user onboarding
4. Submit a few check-ins using the test script or UI
5. View the progress chart on the dashboard

### Test Script
You can use the existing test scripts to create check-in data:

```bash
# From the project root
./test-checkin.sh
```

## Future Enhancements

Potential improvements:
- Export data to CSV/PDF
- Set goals and target lines on charts
- Add trend analysis and predictions
- Photo comparison feature
- Weekly/monthly comparison views
- Notes/journal entries per check-in
- Reminder notifications for check-in day

