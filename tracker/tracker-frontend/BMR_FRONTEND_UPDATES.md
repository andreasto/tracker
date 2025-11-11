# Frontend Updates for BMR Calculation Feature

## Overview
The frontend has been updated to collect the required information for BMR (Basal Metabolic Rate) calculation and display the calculated values to users.

## Changes Made

### 1. Questionnaire Component (`src/components/Questionnaire.tsx`)

**New Required Fields:**
- **Gender**: Dropdown selection (Male/Female)
- **Age**: Number input field
- **Height**: Number input field (in centimeters)
- **Activity Level**: Updated dropdown with standardized options

**Updated Features:**
- Added TypeScript interface for `Question` type with proper typing
- Support for `number` input type
- Visual indicator (*) for required fields
- Updated validation to only require fields marked as `required: true`
- Proper placeholder text for number fields

**Question Order:**
1. Gender (required) ⭐
2. Age (required) ⭐
3. Height in cm (required) ⭐
4. Activity Level (required) ⭐
5. Goal (optional)
6. Experience (optional)
7. Motivation (optional)

**Activity Level Options:**
- Sedentary
- Lightly Active
- Moderately Active
- Very Active
- Extra Active

### 2. API Service (`src/services/api.ts`)

**Updated User Interface:**
```typescript
export interface User {
  userId: string
  name: string
  email: string
  onboardingState: string
  questionnaireAnswers?: Record<string, string>
  startWeight?: number
  measurements?: Record<string, number>
  bmrBase?: number                    // ✨ NEW
  bmrWithActivityLevel?: number       // ✨ NEW
}
```

### 3. Completion Screen (`src/components/CompletionScreen.tsx`)

**New Features:**
- Fetches user data when component loads
- Fetches updated user data after completing onboarding (to get calculated BMR)
- Displays BMR values in an attractive card before the "Go to Dashboard" button

**BMR Display:**
- Base BMR with "kcal/day" label
- Daily Target (BMR with activity level) with "kcal/day" label
- Visual styling with gradient background and colored text

### 4. Dashboard Component (`src/components/Dashboard.tsx`)

**New Section:**
- **"Your Calorie Needs"** card displaying:
  - Base Metabolic Rate (BMR) - calories burned at rest
  - Daily Calorie Target - including activity level
  - Rounded to nearest whole number
  - Highlighted with blue/green color scheme
  - Emoji indicator (💪) for visual appeal

## User Experience Flow

1. **Registration**: User creates account with name and email
2. **Questionnaire**: User provides:
   - Gender (Male/Female)
   - Age (e.g., 30)
   - Height in cm (e.g., 180)
   - Activity Level (Sedentary to Extra Active)
   - Optional: goals, experience, motivation
3. **Start Values**: User enters starting weight and measurements
4. **Completion Screen**: 
   - Shows calculated BMR and daily target
   - User completes onboarding
5. **Dashboard**: 
   - Displays all profile information
   - Shows BMR calculations prominently

## Visual Design

### Questionnaire
- Clean form layout with clear labels
- Required field indicators (*)
- Number inputs with min validation
- Consistent styling with existing design system

### BMR Display (Completion Screen)
- Gradient background (green-50 to blue-50)
- Two-column grid layout
- Large, bold numbers for calorie values
- Small descriptive text
- Green border for emphasis

### BMR Display (Dashboard)
- Blue-themed card matching other info cards
- Stacked layout with visual separation
- Color-coded values (blue for BMR, green for target)
- Descriptive subtitles explaining each value

## Example Data Flow

**Input:**
```json
{
  "gender": "male",
  "age": "30",
  "height": "180",
  "activityLevel": "moderately active",
  "startWeight": 75.5
}
```

**Output (displayed on UI):**
```
Base BMR: 1741 kcal/day
Daily Target: 2699 kcal/day
```

## Testing the Changes

1. Start the development server:
   ```bash
   cd tracker-frontend
   npm run dev
   ```

2. Complete the onboarding flow:
   - Enter name and email
   - Fill in the questionnaire (including gender, age, height)
   - Provide start weight and measurements
   - Complete onboarding

3. Verify BMR is displayed:
   - Check completion screen shows calculated values
   - Check dashboard shows BMR information card

## Browser Compatibility

All changes use standard HTML5 input types and modern CSS that works in:
- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)

## Accessibility

- All form fields have proper labels
- Required fields are marked visually and semantically
- Number inputs have appropriate constraints (min="1", step="1")
- Color is not the only indicator (text labels provided)

## Future Enhancements

Consider adding:
- Imperial unit conversion (feet/inches, pounds)
- BMR calculation method selection (Harris-Benedict vs Mifflin-St Jeor)
- Progress tracking against BMR targets
- BMR recalculation when weight changes
- Visual charts showing calorie distribution
- Tooltips explaining BMR and activity levels

