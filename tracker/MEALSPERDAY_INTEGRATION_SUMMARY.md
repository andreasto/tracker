# MealsPerDay Integration - Summary

## ✅ Changes Completed

### Frontend Changes
**File:** `tracker-frontend/src/components/Questionnaire.tsx`

**Change:** Added new required question to the questionnaire:

```typescript
{
  id: 'mealsPerDay',
  label: 'How many meals do you prefer per day?',
  type: 'select',
  options: ['3', '4', '5'],
  required: true
}
```

**Position:** After "How active are you?" question, before "What is your primary goal?"

**Build Status:** ✅ Frontend builds successfully

---

### Backend Changes

**No code changes required!** 

The backend already supports this field through:
1. **UserActor** stores all questionnaire answers in a `Dictionary<string, string>`
2. **MealPlanService** retrieves `mealsPerDay` from questionnaire answers
3. **MealPlanController** uses the value when creating meal plans

The backend gracefully handles:
- ✅ When `mealsPerDay` is provided → uses that value (3, 4, or 5)
- ✅ When `mealsPerDay` is missing → defaults to 3 meals
- ✅ When value is invalid → defaults to 3 meals

**Build Status:** ✅ Backend builds successfully

---

## How It Works

### User Flow

1. **User registers** → State: `Registered`
2. **User answers questionnaire** (including `mealsPerDay`) → State: `QuestionnaireAnswered`
3. **User provides start values** → State: `StartValuesProvided`
4. **User completes onboarding** → BMR calculated → State: `Complete`
5. **User creates meal plan** → System retrieves `mealsPerDay` from questionnaire and uses it

### Data Flow

```
User fills questionnaire
    ↓
mealsPerDay = "4"
    ↓
Stored in QuestionnaireAnswers dictionary
    ↓
User completes onboarding
    ↓
User creates meal plan
    ↓
MealPlanService reads mealsPerDay from QuestionnaireAnswers
    ↓
Creates 4 meals per day (breakfast, lunch, snack, dinner)
    ↓
Distributes calories: 25%, 35%, 10%, 30%
```

---

## Testing

### Run the Test Script

```bash
./test-mealplan.sh
```

This script:
1. Creates a user
2. Answers questionnaire with `mealsPerDay: "4"`
3. Provides start values
4. Completes onboarding (BMR calculated)
5. Creates a meal plan (automatically uses 4 meals per day)
6. Retrieves the meal plan details

### Expected Output

When creating the meal plan, you should see:
```json
{
  "mealPlanId": 1,
  "message": "Meal plan created successfully with 4 meals per day"
}
```

When retrieving meal plan details, each day should have 4 meals:
- Breakfast (25% of daily calories)
- Lunch (35% of daily calories)
- Snack (10% of daily calories)
- Dinner (30% of daily calories)

---

## Meal Distribution by Count

### 3 Meals (Default)
```
Breakfast: 30% (e.g., 750 cal if BMR = 2500)
Lunch:     40% (e.g., 1000 cal)
Dinner:    30% (e.g., 750 cal)
```

### 4 Meals
```
Breakfast: 25% (e.g., 625 cal if BMR = 2500)
Lunch:     35% (e.g., 875 cal)
Snack:     10% (e.g., 250 cal)
Dinner:    30% (e.g., 750 cal)
```

### 5 Meals
```
Breakfast:      25% (e.g., 625 cal if BMR = 2500)
Morning Snack:  10% (e.g., 250 cal)
Lunch:          30% (e.g., 750 cal)
Afternoon Snack:10% (e.g., 250 cal)
Dinner:         25% (e.g., 625 cal)
```

---

## API Integration

### Questionnaire Endpoint
```bash
curl -X POST "http://localhost:5000/User/user-123/questionnaire" \
  -H "Content-Type: application/json" \
  -d '{
    "answers": {
      "gender": "male",
      "age": "30",
      "height": "180",
      "activityLevel": "moderately active",
      "mealsPerDay": "4"
    }
  }'
```

### Meal Plan Creation
```bash
# The mealsPerDay value is automatically retrieved from the user's questionnaire
curl -X POST "http://localhost:5000/MealPlan/user-123" \
  -H "Content-Type: application/json" \
  -d '{
    "planName": "Weekly Plan",
    "startDate": "2025-11-12T00:00:00Z",
    "endDate": "2025-11-18T23:59:59Z"
  }'
```

---

## Files Modified

### Frontend
- ✅ `tracker-frontend/src/components/Questionnaire.tsx` - Added mealsPerDay question

### Backend
- ✅ No changes needed (already supports it)

### Documentation
- ✅ `ONBOARDING_WITH_MEALSPERDAY.md` - Complete onboarding documentation
- ✅ `test-mealplan.sh` - Updated test script header

### Previously Created (Still Valid)
- ✅ `MEALPLAN_FEATURE.md` - Complete meal plan feature documentation
- ✅ `MEALPLAN_API_REFERENCE.md` - API quick reference
- ✅ `MEALPLAN_IMPLEMENTATION_SUMMARY.md` - Implementation details

---

## Validation

### Frontend Validation
- ✅ Field is required
- ✅ Only allows values: "3", "4", "5"
- ✅ Form cannot be submitted without this field

### Backend Validation
- ✅ Accepts any string value
- ✅ Parses to integer when creating meal plans
- ✅ Validates range (3-5) when parsing
- ✅ Falls back to 3 if invalid or missing

---

## User Experience

### In the Frontend
1. User sees the questionnaire after registration
2. The "How many meals do you prefer per day?" question appears
3. User can select 3, 4, or 5 from a dropdown
4. Field is marked as required (red asterisk)
5. User cannot proceed without selecting a value

### In the Backend
1. Value is stored in the user's questionnaire answers
2. Value is retrieved when creating meal plans
3. Appropriate number of meals are created per day
4. Calories are distributed according to meal plan defaults

---

## Summary

✅ **Frontend Updated:** Questionnaire now includes mealsPerDay field  
✅ **Backend Compatible:** Already handles the field automatically  
✅ **Fully Integrated:** Works seamlessly with meal plan creation  
✅ **Well Documented:** Complete documentation provided  
✅ **Tested:** Test script demonstrates the full flow  

**Status: COMPLETE** 🎉

The onboarding now includes mealsPerDay in both frontend and backend, and it's fully integrated with the meal plan feature!

