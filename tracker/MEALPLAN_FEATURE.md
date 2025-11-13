# Meal Plan Feature Documentation

## Overview

The meal plan feature allows users to create personalized meal plans based on their BMR (Basal Metabolic Rate) with activity level and their preferred number of meals per day.

## Database Schema

The meal plan feature uses the following tables (already created via FluentMigrator):

- **mealplans**: Stores the overall meal plan for a user
- **mealplan_days**: Stores individual days within a meal plan
- **mealplan_meals**: Stores individual meals (breakfast, lunch, dinner, snack) for each day
- **mealplan_recipes**: Junction table linking meals to recipes (for future use)

## Prerequisites

Before creating a meal plan, a user must:

1. Be created in the system
2. Complete the onboarding process (answer questionnaire, provide start values, complete onboarding)
3. Have a calculated `bmrWithActivityLevel` value

## Questionnaire Fields

The questionnaire should include the following field for meal planning:

- **mealsPerDay** (string): Number of meals the user wants per day (minimum 3)
  - Supported values: "3", "4", "5"
  - Default: "3" (breakfast, lunch, dinner)

## Meal Distribution

The system automatically distributes daily calories across meals based on the `mealsPerDay` setting:

### 3 Meals Per Day
- Breakfast: 30%
- Lunch: 40%
- Dinner: 30%

### 4 Meals Per Day
- Breakfast: 25%
- Lunch: 35%
- Snack: 10%
- Dinner: 30%

### 5 Meals Per Day
- Breakfast: 25%
- Morning Snack: 10%
- Lunch: 30%
- Afternoon Snack: 10%
- Dinner: 25%

## API Endpoints

### Create Meal Plan
```http
POST /MealPlan/{userId}
Content-Type: application/json

{
  "planName": "Weekly Meal Plan",
  "startDate": "2025-11-12T00:00:00Z",
  "endDate": "2025-11-18T23:59:59Z"  // optional, defaults to 7 days from startDate
}
```

**Response:**
```json
{
  "mealPlanId": 1,
  "message": "Meal plan created successfully with 4 meals per day"
}
```

### Get Meal Plan
```http
GET /MealPlan/{mealPlanId}
```

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

### Get User's Meal Plans
```http
GET /MealPlan/user/{userId}
```

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
  }
]
```

## How It Works

1. **User Validation**: The controller verifies that:
   - The user exists
   - The user has completed onboarding
   - The user has a calculated BMR with activity level

2. **Meal Count Detection**: The system extracts the `mealsPerDay` value from the user's questionnaire answers, defaulting to 3 if not provided.

3. **Plan Creation**: The service:
   - Creates the meal plan record
   - Generates meal plan days for the specified date range
   - Creates meals for each day based on the meal distribution
   - Calculates calorie targets for each meal

4. **Calorie Distribution**: Each meal's calorie target is calculated as:
   ```
   meal_calories = bmrWithActivityLevel * meal_percentage
   ```

## Example Workflow

```bash
# 1. Create user
curl -X POST "http://localhost:5000/User/user-123" \
  -H "Content-Type: application/json" \
  -d '{"name": "John Doe", "email": "john@example.com"}'

# 2. Answer questionnaire with mealsPerDay
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

# 3. Provide start values
curl -X POST "http://localhost:5000/User/user-123/start-values" \
  -H "Content-Type: application/json" \
  -d '{
    "startWeight": 80.0,
    "measurements": {"chest": 100.0, "waist": 85.0, "hips": 95.0}
  }'

# 4. Complete onboarding
curl -X POST "http://localhost:5000/User/user-123/complete-onboarding"

# 5. Create meal plan
curl -X POST "http://localhost:5000/MealPlan/user-123" \
  -H "Content-Type: application/json" \
  -d '{
    "planName": "My Weekly Plan",
    "startDate": "2025-11-12T00:00:00Z",
    "endDate": "2025-11-18T23:59:59Z"
  }'
```

## Implementation Details

### Domain Models
- **MealPlanCommands.cs**: Command objects for meal plan operations
- **MealPlanModels.cs**: Data models and meal distribution defaults
- **IWithMealPlanId.cs**: Marker interface for meal plan-related entities

### Service Layer
- **MealPlanService.cs**: Business logic for creating and retrieving meal plans
  - Uses Dapper for database operations
  - Implements transaction management
  - Handles calorie calculations

### Controller
- **MealPlanController.cs**: REST API endpoints for meal plan operations
  - Integrates with UserActor to fetch user data
  - Validates user state before creating plans

## Future Enhancements

1. **Recipe Assignment**: Automatically assign recipes to meals based on calorie targets
2. **Meal Preferences**: Allow users to specify dietary preferences (vegetarian, vegan, etc.)
3. **Meal Templates**: Pre-defined meal plan templates based on goals
4. **Shopping Lists**: Generate shopping lists from meal plans
5. **Meal Swap**: Allow users to swap meals within their plan
6. **Macro Tracking**: Track protein, fat, and carbohydrate distribution

## Testing

Use the provided test script:

```bash
./test-mealplan.sh
```

This script will:
1. Create a test user
2. Complete the onboarding process
3. Create a meal plan
4. Retrieve the meal plan

## Notes

- Meal plans are stored in PostgreSQL, not in the Akka event journal
- The `user_id` in the mealplans table corresponds to the Akka persistence ID format (e.g., "user-123")
- If no end date is provided, the system creates a 7-day meal plan by default
- All dates are stored as date-only values (time component is ignored)

