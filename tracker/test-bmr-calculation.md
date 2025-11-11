# BMR Calculation Verification

## Implementation Summary

The BMR (Basal Metabolic Rate) calculation has been successfully integrated into the User onboarding flow.

### Changes Made:

1. **User Record** - Added `BmrBase` and `BmrWithActivityLevel` fields to store calculated values
2. **BmrCalculator Class** - New static class that calculates BMR based on user data:
   - Extracts gender, age, height from questionnaire answers
   - Uses start weight from ProvideStartValues
   - Applies gender-specific formulas:
     - **Male**: BMR = 66.5 + (13.75 × weight) + (5.003 × height) - (6.75 × age)
     - **Female**: BMR = 655.1 + (9.563 × weight) + (1.850 × height) - (4.676 × age)
   - Applies activity level multipliers:
     - Sedentary: 1.2
     - Lightly Active: 1.375
     - Moderately Active: 1.55
     - Very Active: 1.725
     - Extra Active: 1.9

3. **CompleteOnboardingCommand** - Automatically calculates BMR when completing onboarding
4. **UserOnboardingCompleted Event** - Stores the calculated BMR values
5. **Controller** - No longer requires BMR in request body

### Required Questionnaire Fields:

The questionnaire must now include:
- `gender`: "male" or "female" (or "m"/"f")
- `age`: numeric value
- `height`: numeric value in centimeters
- `activityLevel`: "sedentary", "lightly active", "moderately active", "very active", or "extra active"

### Example Calculation:

For a 30-year-old male, 180cm tall, weighing 75.5kg with moderate activity:
- BMR Base: 66.5 + (13.75 × 75.5) + (5.003 × 180) - (6.75 × 30) = **1741.26 kcal/day**
- BMR with Activity (moderate = 1.55): 1741.26 × 1.55 = **2698.96 kcal/day**

### API Usage:

```json
POST /user/{userId}/questionnaire
{
  "answers": {
    "gender": "male",
    "age": "30",
    "height": "180",
    "activityLevel": "moderate",
    "goals": "weight-loss",
    "dietaryRestrictions": "none"
  }
}

POST /user/{userId}/start-values
{
  "startWeight": 75.5,
  "measurements": {
    "chest": 95.0,
    "waist": 85.0,
    "hips": 100.0
  }
}

POST /user/{userId}/complete-onboarding
// No body needed - BMR is calculated automatically
```

The response will include the calculated BMR values in the User record.

