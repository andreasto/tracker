# Quick Start - Progress Tracking Feature

## Get Started in 3 Steps

### Step 1: Start the Application
```bash
# Terminal 1: Start Backend
cd src/tracker.App
dotnet run

# Terminal 2: Start Frontend
cd tracker-frontend
npm run dev
```

### Step 2: Create Test Data
```bash
# Terminal 3: Run test script
./test-progress-tracking.sh
```

The script will output a URL like:
```
http://localhost:5173/dashboard/test-user-progress-1699800000
```

### Step 3: View the Progress Chart
Open the URL in your browser and you'll see:
- 📊 Interactive progress chart with 5 weeks of data
- 📉 Weight trend from 85.0 kg → 81.5 kg
- 📏 All measurements showing downward trend
- 🎨 Beautiful color-coded visualization

## Using the Feature

### View Progress
1. **Toggle Measurements**: Click the colored buttons to show/hide different metrics
2. **Hover for Details**: Hover over data points to see exact values
3. **View Stats**: Check the summary cards below the chart

### Add New Check-In
1. Click **"📊 New Check-In"** button
2. Fill in all measurement fields:
   - Weight (kg)
   - Chest, Waist, Stomach, Hips, Glutes, Thigh, Overarm (cm)
3. Click **"Submit Check-In"**
4. Chart automatically updates with your new data

## What You'll See

### Progress Chart Shows:
- ✅ Weight and measurement trends over time
- ✅ Multiple metrics on one chart
- ✅ Interactive toggles to compare different measurements
- ✅ Stats cards showing current value and change
- ✅ Percentage change from start

### Example Data:
The test script creates a user who has lost:
- **3.5 kg** in weight (-4.1%)
- **5.0 cm** in waist (-5.6%)
- Progressive improvements in all measurements

## Troubleshooting

### Backend not running?
```bash
cd src/tracker.App
dotnet run
# Should start on http://localhost:5000
```

### Frontend not running?
```bash
cd tracker-frontend
npm run dev
# Should start on http://localhost:5173
```

### Test script fails?
Make sure both backend and PostgreSQL are running:
```bash
# Check if backend is responding
curl http://localhost:5000/health

# Check PostgreSQL
./check-database.sh
```

### Chart not showing data?
1. Check browser console for errors
2. Verify API is returning data:
   ```bash
   curl http://localhost:5000/checkin/{userId}
   ```
3. Refresh the page

## File Locations

- **Progress Chart Component**: `tracker-frontend/src/components/ProgressChart.tsx`
- **Check-In Form**: `tracker-frontend/src/components/CheckInForm.tsx`
- **Dashboard**: `tracker-frontend/src/components/Dashboard.tsx`
- **API Service**: `tracker-frontend/src/services/api.ts`
- **Test Script**: `test-progress-tracking.sh`

## Documentation

For more details, see:
- 📖 **Feature Documentation**: `tracker-frontend/PROGRESS_TRACKING_FEATURE.md`
- 🔍 **Quick Reference**: `tracker-frontend/PROGRESS_TRACKING_QUICK_REFERENCE.md`
- 🎨 **Visual Guide**: `tracker-frontend/PROGRESS_TRACKING_VISUAL_GUIDE.md`
- ✅ **Checklist**: `PROGRESS_TRACKING_CHECKLIST.md`
- 📝 **Summary**: `PROGRESS_TRACKING_SUMMARY.md`

## Customization

### Change Chart Colors
Edit `ProgressChart.tsx`:
```typescript
const measurementColors = {
  weight: '#your-color',
  // ...
}
```

### Change Chart Height
Edit `ProgressChart.tsx`:
```typescript
<ResponsiveContainer width="100%" height={400}> // Change height
```

### Modify Measurements
Edit both:
- `ProgressChart.tsx`: measurement labels and colors
- `CheckInForm.tsx`: form fields
- Backend API: measurement fields

---

**That's it!** You're ready to track progress with beautiful, interactive charts. 🎉

