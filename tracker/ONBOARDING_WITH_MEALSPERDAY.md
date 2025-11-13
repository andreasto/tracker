# Onboarding Flow with MealsPerDay

## Overview
The onboarding flow now includes the `mealsPerDay` questionnaire field, which is used to create personalized meal plans.

## Onboarding Steps

### 1. User Registration
**Endpoint:** `POST /User/{userId}`

**Request:**
```json
{
  "name": "John Doe",
  "email": "john@example.com"
}
```

**State:** `NotStarted` → `Registered`

---

### 2. Questionnaire
**Endpoint:** `POST /User/{userId}/questionnaire`

**Required Fields:**
- `gender` - "male" or "female"
- `age` - number (e.g., "30")
- `height` - in cm (e.g., "180")
- `activityLevel` - "sedentary", "lightly active", "moderately active", "very active", "extra active"
- `mealsPerDay` - "3", "4", or "5" ⭐ **NEW**

**Optional Fields:**
- `goal` - User's fitness goal
- `experience` - Tracking experience level
- `motivation` - What motivates the user

**Request:**
```json
{
  "answers": {
    "gender": "male",
    "age": "30",
    "height": "180",
    "activityLevel": "moderately active",
    "mealsPerDay": "4",
    "goal": "maintain",
    "experience": "beginner",
    "motivation": "Health and fitness"
  }
}
```

**State:** `Registered` → `QuestionnaireAnswered`

---

### 3. Start Values
**Endpoint:** `POST /User/{userId}/start-values`

**Request:**
```json
{
  "startWeight": 80.0,
  "measurements": {
    "chest": 100.0,
    "waist": 85.0,
    "hips": 95.0
  }
}
```

**State:** `QuestionnaireAnswered` → `StartValuesProvided`

---

### 4. Complete Onboarding
**Endpoint:** `POST /User/{userId}/complete-onboarding`

**What Happens:**
- System calculates BMR using Mifflin-St Jeor Equation
- BMR is multiplied by activity level to get total daily calories
- User state becomes `Complete`

**Response:**
```json
{
  "userId": "user-123",
  "isSuccess": true,
  "event": {
    "userId": "user-123",
    "bmrBase": 1850.5,
    "bmrWithActivityLevel": 2868.27
  }
}
```

**State:** `StartValuesProvided` → `Complete`

---

## Frontend Implementation

The frontend questionnaire includes a new required field:

```typescript
{
  id: 'mealsPerDay',
  label: 'How many meals do you prefer per day?',
  type: 'select',
  options: ['3', '4', '5'],
  required: true
}
```

### Meal Distribution

Based on the selected `mealsPerDay`, the system will distribute calories as follows:

**3 Meals Per Day:**
- Breakfast: 30%
- Lunch: 40%
- Dinner: 30%

**4 Meals Per Day:**
- Breakfast: 25%
- Lunch: 35%
- Snack: 10%
- Dinner: 30%

**5 Meals Per Day:**
- Breakfast: 25%
- Morning Snack: 10%
- Lunch: 30%
- Afternoon Snack: 10%
- Dinner: 25%

---

## Using MealsPerDay in Meal Plans

After onboarding is complete, when creating a meal plan:

**Endpoint:** `POST /MealPlan/{userId}`

```json
{
  "planName": "Weekly Plan",
  "startDate": "2025-11-12T00:00:00Z",
  "endDate": "2025-11-18T23:59:59Z"
}
```

The system will:
1. Retrieve the user's `mealsPerDay` from their questionnaire answers
2. Use the user's `bmrWithActivityLevel` for daily calorie target
3. Create meals for each day based on the selected meal frequency
4. Distribute calories according to the meal distribution percentages

**Example Response:**
```json
{
  "mealPlanId": 1,
  "message": "Meal plan created successfully with 4 meals per day"
}
```

---

## Complete Example Flow

```bash
# 1. Create user
curl -X POST "http://localhost:5000/User/user-123" \
  -H "Content-Type: application/json" \
  -d '{"name": "John Doe", "email": "john@example.com"}'

# 2. Answer questionnaire (including mealsPerDay)
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

# 3. Provide start values
curl -X POST "http://localhost:5000/User/user-123/start-values" \
  -H "Content-Type: application/json" \
  -d '{
    "startWeight": 80.0,
    "measurements": {"chest": 100.0, "waist": 85.0, "hips": 95.0}
  }'

# 4. Complete onboarding
curl -X POST "http://localhost:5000/User/user-123/complete-onboarding"

# 5. Create meal plan (automatically uses mealsPerDay from questionnaire)
curl -X POST "http://localhost:5000/MealPlan/user-123" \
  -H "Content-Type: application/json" \
  -d '{
    "planName": "My Weekly Plan",
    "startDate": "2025-11-12T00:00:00Z",
    "endDate": "2025-11-18T23:59:59Z"
  }'
```

---

## Backend Validation

The backend automatically:
- Stores `mealsPerDay` as part of questionnaire answers
- Validates it's a number between 3 and 5 when creating meal plans
- Defaults to 3 meals if not provided
- Uses it to determine meal distribution when creating meal plans

No additional backend changes are needed - the field is stored in the questionnaire answers dictionary and retrieved when creating meal plans.

---

## Frontend UI

The questionnaire form now includes:
- **Position:** After "How active are you?" and before "What is your primary goal?"
- **Type:** Dropdown/Select
- **Options:** 3, 4, or 5 meals
- **Required:** Yes
- **Label:** "How many meals do you prefer per day?"

---

## Summary

✅ **Frontend:** Added `mealsPerDay` field to questionnaire
✅ **Backend:** Already supports storing any questionnaire answers
✅ **Meal Plan Service:** Retrieves and uses `mealsPerDay` from user questionnaire
✅ **Default Behavior:** Falls back to 3 meals if not specified
✅ **Documentation:** Updated with complete onboarding flow

The integration is complete and seamless - users answer the question during onboarding, and the system automatically uses it when creating meal plans!

