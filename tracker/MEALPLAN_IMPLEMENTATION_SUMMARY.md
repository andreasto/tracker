# Meal Plan Feature - Implementation Summary

## ✅ What Was Implemented

### 1. Domain Models (`src/tracker.Domain/MealPlan/`)
- **IWithMealPlanId.cs** - Marker interface for meal plan entities
- **MealPlanCommands.cs** - Command objects for meal plan operations
- **MealPlanModels.cs** - Core data models including:
  - `MealPlan` - Main meal plan record
  - `MealPlanDay` - Individual day in a plan
  - `MealPlanMeal` - Individual meal (breakfast, lunch, etc.)
  - `MealPlanDefaults` - Calorie distribution logic for 3, 4, and 5 meals per day

### 2. Service Layer (`src/tracker.App/Services/`)
- **MealPlanService.cs** - Business logic implementation:
  - `CreateMealPlanAsync()` - Creates a meal plan with all days and meals
  - `GetMealPlanAsync()` - Retrieves basic meal plan info
  - `GetUserMealPlansAsync()` - Lists all meal plans for a user
  - `GetMealPlanDetailsAsync()` - Gets complete plan with all days and meals

### 3. API Layer (`src/tracker.App/Controllers/`)
- **MealPlanController.cs** - REST API endpoints:
  - `POST /MealPlan/{userId}` - Create meal plan
  - `GET /MealPlan/{mealPlanId}` - Get meal plan summary
  - `GET /MealPlan/{mealPlanId}/details` - Get complete meal plan
  - `GET /MealPlan/user/{userId}` - Get all user's meal plans

### 4. Configuration
- **Directory.Packages.props** - Added Dapper 2.1.35 package
- **tracker.App.csproj** - Added Dapper reference
- **Program.cs** - Registered `IMealPlanService` in DI container

### 5. Database
- Uses existing migration `20241111001_InitialRecipeAndMealPlanningTables.cs`
- Tables already created:
  - `mealplans`
  - `mealplan_days`
  - `mealplan_meals`
  - `mealplan_recipes` (for future use)

### 6. Documentation
- **MEALPLAN_FEATURE.md** - Complete feature documentation
- **MEALPLAN_API_REFERENCE.md** - API endpoint quick reference
- **test-mealplan.sh** - Executable test script

## 🎯 Key Features

### Automatic Calorie Distribution
The system automatically distributes daily calories based on the user's `mealsPerDay` questionnaire answer:

- **3 meals**: Breakfast (30%), Lunch (40%), Dinner (30%)
- **4 meals**: Breakfast (25%), Lunch (35%), Snack (10%), Dinner (30%)
- **5 meals**: Breakfast (25%), Snack (10%), Lunch (30%), Snack (10%), Dinner (25%)

### BMR Integration
- Meal plans are based on the user's `bmrWithActivityLevel`
- System validates that user has completed onboarding before creating plans
- Each meal's calorie target is calculated automatically

### Flexible Date Ranges
- Support for custom start and end dates
- Defaults to 7 days if no end date is provided
- Generates all days and meals within the date range

## 🔄 User Flow

1. **User completes onboarding** (including questionnaire with `mealsPerDay` field)
2. **System calculates BMR** with activity level
3. **User creates meal plan** via API
4. **System automatically**:
   - Retrieves user's BMR and meals per day preference
   - Creates meal plan record
   - Generates days between start and end date
   - Creates meals for each day with calorie targets
5. **User can retrieve** meal plan summary or full details

## 📊 Data Flow

```
User (Akka Event Journal)
  ↓
  ├─ BMR With Activity Level
  └─ Questionnaire Answers (mealsPerDay)
     ↓
MealPlan Creation
     ↓
PostgreSQL Tables
  ├─ mealplans (plan metadata)
  ├─ mealplan_days (one per day)
  └─ mealplan_meals (3-5 per day)
```

## 🧪 Testing

Run the test script to verify the complete flow:

```bash
./test-mealplan.sh
```

This script will:
1. Create a user
2. Complete onboarding with 4 meals per day
3. Create a weekly meal plan
4. Retrieve the meal plan

## 📝 Example Request/Response

**Create Meal Plan:**
```bash
curl -X POST "http://localhost:5000/MealPlan/user-123" \
  -H "Content-Type: application/json" \
  -d '{
    "planName": "Weekly Plan",
    "startDate": "2025-11-12T00:00:00Z",
    "endDate": "2025-11-18T23:59:59Z"
  }'
```

**Response:**
```json
{
  "mealPlanId": 1,
  "message": "Meal plan created successfully with 4 meals per day"
}
```

**Get Details:**
```bash
curl -X GET "http://localhost:5000/MealPlan/1/details"
```

Returns complete plan with all 7 days and 28 meals (4 per day × 7 days).

## 🚀 Next Steps / Future Enhancements

1. **Recipe Assignment**: Automatically assign recipes to meals based on calorie targets
2. **Meal Customization**: Allow users to modify individual meal calorie targets
3. **Meal Templates**: Pre-defined meal templates based on dietary preferences
4. **Shopping Lists**: Generate grocery lists from meal plans
5. **Meal Swap**: Allow swapping meals between days
6. **Nutrition Tracking**: Track macros (protein, fat, carbs) per meal
7. **Recipe Recommendations**: AI-based recipe suggestions

## ✨ Benefits

- **Automated Planning**: No manual calorie calculations needed
- **Personalized**: Based on individual BMR and preferences
- **Flexible**: Support for different meal frequencies
- **Extensible**: Ready for recipe integration
- **Scalable**: PostgreSQL storage for efficient queries

## 📦 Dependencies Added

- **Dapper 2.1.35** - Lightweight ORM for database queries
  - Added to `Directory.Packages.props`
  - Referenced in `tracker.App.csproj`

## 🔧 Technical Details

- **Transaction Management**: Uses PostgreSQL transactions for data consistency
- **Error Handling**: Comprehensive error handling with rollback support
- **Validation**: Validates user state before creating meal plans
- **Date Handling**: All dates stored as date-only (time component ignored)
- **Ordering**: Meals ordered logically (breakfast → lunch → dinner → snack)

---

**Status**: ✅ Complete and ready to use!

All code compiles successfully and is ready for testing.

