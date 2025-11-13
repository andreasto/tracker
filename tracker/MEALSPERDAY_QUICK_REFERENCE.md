# Quick Reference: MealsPerDay Integration

## What Changed?

### Frontend ✅
Added one new required question to the questionnaire:

```typescript
{
  id: 'mealsPerDay',
  label: 'How many meals do you prefer per day?',
  type: 'select',
  options: ['3', '4', '5'],
  required: true
}
```

**File:** `tracker-frontend/src/components/Questionnaire.tsx`

### Backend ✅
No changes needed! Already supports it.

---

## How to Use

### 1. User Completes Questionnaire
```javascript
{
  "answers": {
    "gender": "male",
    "age": "30",
    "height": "180",
    "activityLevel": "moderately active",
    "mealsPerDay": "4"  // ← NEW FIELD
  }
}
```

### 2. System Creates Meal Plan
```javascript
POST /MealPlan/user-123
{
  "planName": "Weekly Plan",
  "startDate": "2025-11-12T00:00:00Z"
}

// Response:
{
  "mealPlanId": 1,
  "message": "Meal plan created successfully with 4 meals per day"
}
```

### 3. Result
Each day gets 4 meals with calorie distribution:
- Breakfast: 25%
- Lunch: 35%
- Snack: 10%
- Dinner: 30%

---

## Meal Distributions

| Meals | Breakfast | Lunch | Dinner | Snack(s) |
|-------|-----------|-------|--------|----------|
| **3** | 30% | 40% | 30% | - |
| **4** | 25% | 35% | 30% | 10% |
| **5** | 25% | 30% | 25% | 10% + 10% |

---

## Test It

```bash
./test-mealplan.sh
```

---

## Status

✅ Frontend updated  
✅ Backend compatible  
✅ Fully tested  
✅ Documentation complete  

**Ready to use!**

