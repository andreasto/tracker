# Progress Tracking - Visual Guide

## Dashboard with Progress Chart

### Layout Overview

```
┌─────────────────────────────────────────────────────────────────┐
│  Dashboard                              [📊 New Check-In]       │
│  Welcome back, User Name!                                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Progress Tracking                                              │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ [Weight] [Chest] [Waist] [Stomach] [Hips] [Glutes]      │  │
│  │ [Thigh] [Overarm]  ← Toggle buttons (selected = colored)│  │
│  └──────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                                                          │  │
│  │  85├─────●                                               │  │
│  │    │      ╲                                              │  │
│  │  84│       ●                                             │  │
│  │    │        ╲                                            │  │
│  │  83│         ●                                           │  │
│  │    │          ╲                                          │  │
│  │  82│           ●                                         │  │
│  │    │            ╲                                        │  │
│  │  81│             ●                                       │  │
│  │    └─────┬────┬────┬────┬────                           │  │
│  │        Week1 Week2 Week3 Week4 Week5                     │  │
│  │                                                          │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐             │
│  │ Weight      │ │ Waist       │ │ Chest       │             │
│  │ 81.5        │ │ 85.0        │ │ 102.5       │             │
│  │ -3.5 (-4.1%)│ │ -5.0 (-5.6%)│ │ -2.5 (-2.4%)│             │
│  └─────────────┘ └─────────────┘ └─────────────┘             │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  Profile                          Your Goals                    │
│  User ID: test-user-123          Gender: Male                  │
│  Name: Test User                 Age: 30                       │
│  Email: test@example.com         Goal: Lose Weight             │
│                                                                 │
│  Starting Stats                  Measurements                  │
│  Weight: 85.0 kg                Chest: 105.0 cm               │
│                                  Waist: 90.0 cm               │
│                                                                 │
│  💪 Your Calorie Needs                                          │
│  Base Metabolic Rate (BMR)                                     │
│  1,850 kcal/day                                                │
│  Calories burned at rest                                       │
│                                                                 │
│  Daily Calorie Target                                          │
│  2,590 kcal/day                                                │
│  Including activity level                                      │
└─────────────────────────────────────────────────────────────────┘
```

## Check-In Form Modal

When clicking "📊 New Check-In", a modal appears:

```
┌────────────────────────────────────────────────────────┐
│ Weekly Check-In                               [×]      │
├────────────────────────────────────────────────────────┤
│                                                        │
│  ⚖️  Weight (kg)         💪 Chest (cm)                │
│  ┌──────────────────┐   ┌──────────────────┐          │
│  │ 81.5             │   │ 102.5            │          │
│  └──────────────────┘   └──────────────────┘          │
│                                                        │
│  🎯 Waist (cm)          🎯 Stomach (cm)               │
│  ┌──────────────────┐   ┌──────────────────┐          │
│  │ 85.0             │   │ 84.0             │          │
│  └──────────────────┘   └──────────────────┘          │
│                                                        │
│  📏 Hips (cm)           📏 Glutes (cm)                │
│  ┌──────────────────┐   ┌──────────────────┐          │
│  │ 97.0             │   │ 98.0             │          │
│  └──────────────────┘   └──────────────────┘          │
│                                                        │
│  📏 Thigh (cm)          💪 Overarm (cm)               │
│  ┌──────────────────┐   ┌──────────────────┐          │
│  │ 56.0             │   │ 34.0             │          │
│  └──────────────────┘   └──────────────────┘          │
│                                                        │
│                    [Cancel] [Submit Check-In]          │
└────────────────────────────────────────────────────────┘
```

## Interactive Chart Features

### Measurement Toggle Buttons

**Active (Selected):**
```
┌──────────────┐
│   Weight     │  ← Colored with measurement color (blue)
└──────────────┘
```

**Inactive (Not Selected):**
```
┌──────────────┐
│   Chest      │  ← Gray background
└──────────────┘
```

### Chart Tooltip (on hover)

```
┌─────────────────────┐
│ Week 3              │
│ Weight: 82.8 kg     │
│ Waist: 87.0 cm      │
│ Chest: 103.5 cm     │
└─────────────────────┘
```

### Stats Card

```
┌─────────────────────┐
│ Weight (kg)         │ ← Blue left border
│ 81.5                │ ← Large, bold current value
│ -3.5 (-4.1%)        │ ← Green text (decrease is good)
└─────────────────────┘
```

## Color Coding Reference

Visual representation of measurement colors:

```
Weight  : ████████  #3b82f6 (Blue)
Chest   : ████████  #ef4444 (Red)
Waist   : ████████  #10b981 (Green)
Hips    : ████████  #f59e0b (Amber)
Glutes  : ████████  #8b5cf6 (Purple)
Thigh   : ████████  #ec4899 (Pink)
Stomach : ████████  #14b8a6 (Teal)
Overarm : ████████  #f97316 (Orange)
```

## Responsive Behavior

### Desktop (>1024px)
- Full width chart (up to 1280px container)
- 4-column stats grid
- 2-column profile/goals sections

### Tablet (768px - 1024px)
- Full width chart
- 2-column stats grid
- 2-column profile/goals sections

### Mobile (<768px)
- Full width chart (scrollable if needed)
- 2-column stats grid
- Single column profile/goals sections
- Stacked measurement toggles

## Empty State

When user has no check-ins yet:

```
┌────────────────────────────────────────────┐
│                                            │
│   No check-in data yet.                   │
│   Start tracking your progress!           │
│                                            │
└────────────────────────────────────────────┘
```

## Loading State

While fetching data:

```
┌────────────────────────────────────────────┐
│                                            │
│           ⟳  (spinning animation)          │
│              Loading...                    │
│                                            │
└────────────────────────────────────────────┘
```

## Error State

If data fails to load:

```
┌────────────────────────────────────────────┐
│  ⚠️  Failed to load data                   │
│                                            │
│  Please try again later                   │
└────────────────────────────────────────────┘
```

## User Journey Flow

```
1. User arrives at Dashboard
   ↓
2. Sees progress chart with historical data
   ↓
3. Clicks "📊 New Check-In" button
   ↓
4. Modal form appears with all measurement fields
   ↓
5. User fills in current measurements
   ↓
6. Clicks "Submit Check-In"
   ↓
7. Form shows loading state ("Submitting...")
   ↓
8. On success:
   - Modal closes
   - Chart refreshes with new data point
   - Stats cards update
   ↓
9. User can:
   - Toggle measurements to compare trends
   - Hover over points for exact values
   - View percentage changes in stats cards
```

## Accessibility Features

- ✅ Keyboard navigation support
- ✅ Focus indicators on buttons
- ✅ Semantic HTML structure
- ✅ Proper ARIA labels
- ✅ Color contrast meets WCAG AA standards
- ✅ Responsive touch targets (min 44px)

## Animation & Transitions

- Smooth line animations when data updates
- Fade-in for modal appearance
- Hover effects on buttons
- Active state feedback
- Loading spinner rotation

---

This visual guide helps understand the user interface and interaction patterns of the progress tracking feature.

