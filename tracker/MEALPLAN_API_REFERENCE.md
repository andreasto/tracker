# Meal Plan API Quick Reference

## Endpoints

### 1. Create Meal Plan
Creates a new meal plan for a user based on their BMR and questionnaire answers.

**Endpoint:** `POST /MealPlan/{userId}`

**Request Body:**
```json
{
  "planName": "Weekly Meal Plan",
  "startDate": "2025-11-12T00:00:00Z",
  "endDate": "2025-11-18T23:59:59Z"  // optional
}
```

**Response:**
```json
{
  "mealPlanId": 1,
  "message": "Meal plan created successfully with 4 meals per day"
}
```

**Requirements:**
- User must exist
- User must have completed onboarding
- User must have BMR calculated
- User should have answered `mealsPerDay` in questionnaire (defaults to 3)

---

### 2. Get Meal Plan Summary
Retrieves basic meal plan information.

**Endpoint:** `GET /MealPlan/{mealPlanId}`

**Response:**
```json
{
  "mealPlanId": 1,
  "userId": "user-123",
  "planName": "Weekly Meal Plan",
  "startDate": "2025-11-12T00:00:00Z",
  "endDate": "2025-11-18T23:59:59Z",
  "totalCalories": 2500.0,
  "createdAt": "2025-11-12T10:30:00Z"
}
```

---

### 3. Get Meal Plan Details
Retrieves complete meal plan with all days and meals.

**Endpoint:** `GET /MealPlan/{mealPlanId}/details`

**Response:**
```json
{
  "plan": {
    "mealPlanId": 1,
    "userId": "user-123",
    "planName": "Weekly Meal Plan",
    "startDate": "2025-11-12T00:00:00Z",
    "endDate": "2025-11-18T23:59:59Z",
    "totalCalories": 2500.0,
    "createdAt": "2025-11-12T10:30:00Z"
  },
  "days": [
    {
      "day": {
        "dayId": 1,
        "mealPlanId": 1,
        "planDate": "2025-11-12T00:00:00Z",
        "dayTotalCalories": 2500.0
      },
      "meals": [
        {
          "mealId": 1,
          "dayId": 1,
          "mealType": "breakfast",
          "calorieTarget": 625.0
        },
        {
          "mealId": 2,
          "dayId": 1,
          "mealType": "lunch",
          "calorieTarget": 875.0
        },
        {
          "mealId": 3,
          "dayId": 1,
          "mealType": "snack",
          "calorieTarget": 250.0
        },
        {
          "mealId": 4,
          "dayId": 1,
          "mealType": "dinner",
          "calorieTarget": 750.0
        }
      ]
    }
  ]
}
```

---

### 4. Get User's Meal Plans
Retrieves all meal plans for a specific user.

**Endpoint:** `GET /MealPlan/user/{userId}`

**Response:**
```json
[
  {
    "mealPlanId": 1,
    "userId": "user-123",
    "planName": "Weekly Meal Plan",
    "startDate": "2025-11-12T00:00:00Z",
    "endDate": "2025-11-18T23:59:59Z",
    "totalCalories": 2500.0,
    "createdAt": "2025-11-12T10:30:00Z"
  },
  {
    "mealPlanId": 2,
    "userId": "user-123",
    "planName": "Monthly Plan",
    "startDate": "2025-12-01T00:00:00Z",
    "endDate": "2025-12-31T23:59:59Z",
    "totalCalories": 2500.0,
    "createdAt": "2025-11-13T14:20:00Z"
  }
]
```

---

## Meal Types

- `breakfast`
- `lunch`
- `dinner`
- `snack`

## Example cURL Commands

```bash
# Create meal plan
curl -X POST "http://localhost:5000/MealPlan/user-123" \
  -H "Content-Type: application/json" \
  -d '{
    "planName": "My Plan",
    "startDate": "2025-11-12T00:00:00Z",
    "endDate": "2025-11-18T23:59:59Z"
  }'

# Get meal plan summary
curl -X GET "http://localhost:5000/MealPlan/1"

# Get meal plan details
curl -X GET "http://localhost:5000/MealPlan/1/details"

# Get user's meal plans
curl -X GET "http://localhost:5000/MealPlan/user/user-123"
```

## Integration with User Questionnaire

To specify the number of meals per day, include `mealsPerDay` in the questionnaire answers:

```bash
curl -X POST "http://localhost:5000/User/user-123/questionnaire" \
  -H "Content-Type: application/json" \
  -d '{
    "answers": {
      "gender": "male",
      "age": "30",
      "height": "180",
      "activityLevel": "moderate",
      "goal": "maintain",
      "mealsPerDay": "4"
    }
  }'
```

Supported values: "3", "4", "5" (defaults to "3" if not specified)

