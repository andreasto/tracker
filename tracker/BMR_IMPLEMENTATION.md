# BMR Calculation Feature - Complete Implementation

## Overview
The application now automatically calculates BMR (Basal Metabolic Rate) and BMR with activity level when a user completes the onboarding process. The calculation is done server-side based on data collected during onboarding.

## Implementation Details

### 1. BMR Formulas Implemented

**For Men:**
```
BMR = 66.5 + (13.75 × weight in kg) + (5.003 × height in cm) - (6.75 × age)
```

**For Women:**
```
BMR = 655.1 + (9.563 × weight in kg) + (1.850 × height in cm) - (4.676 × age)
```

**Activity Level Multipliers:**
- Sedentary: 1.2
- Lightly Active: 1.375
- Moderately Active: 1.55
- Very Active: 1.725
- Extra Active: 1.9

### 2. Files Modified

#### Domain Layer
- **`src/tracker.Domain/User/UserCommands.cs`**
  - `CompleteOnboardingCommand` - Simplified to not require BMR parameters

- **`src/tracker.Domain/User/UserEvents.cs`**
  - `UserOnboardingCompleted` - Still contains BMR values but they're calculated, not provided

#### Application Layer
- **`src/tracker.App/Actors/UserActor.cs`**
  - Added `BmrBase` and `BmrWithActivityLevel` fields to `User` record
  - Created `BmrCalculator` static class with `Calculate()` method
  - Updated `ProcessCommand()` to calculate BMR automatically when `CompleteOnboardingCommand` is processed
  - Updated `ApplyEvent()` to store calculated BMR values

- **`src/tracker.App/Controllers/UserController.cs`**
  - Simplified `CompleteOnboarding` endpoint to not require request body
  - Removed `CompleteOnboardingRequest` record

#### Test Layer
- **`tests/tracker.App.Tests/UserActorOnboardingSpecs.cs`**
  - Updated tests to include required questionnaire fields (gender, age, height)
  - Updated tests to verify calculated BMR values
  - Removed BMR parameters from `CompleteOnboardingCommand` calls

### 3. Required Questionnaire Fields

The questionnaire must now include these fields for BMR calculation:

| Field | Type | Valid Values | Example |
|-------|------|--------------|---------|
| `gender` | string | "male", "m", "female", "f" | "male" |
| `age` | string (parsed as int) | Numeric value | "30" |
| `height` | string (parsed as double) | Height in cm | "180" |
| `activityLevel` | string | "sedentary", "lightly active", "moderately active", "very active", "extra active" | "moderate" |

### 4. Data Flow

```
1. User Created → State: Registered
   └─ Store: name, email

2. Questionnaire Answered → State: QuestionnaireAnswered
   └─ Store: gender, age, height, activityLevel, etc.

3. Start Values Provided → State: StartValuesProvided
   └─ Store: startWeight, measurements

4. Complete Onboarding → State: Complete
   └─ Calculate: BMR using stored data
   └─ Store: bmrBase, bmrWithActivityLevel
```

### 5. Error Handling

The `BmrCalculator` throws `InvalidOperationException` with descriptive messages if:
- Questionnaire answers or start weight are missing
- Required fields (gender, age, height, activityLevel) are not present
- Values cannot be parsed (age as int, height as double)
- Invalid gender value (must be male/m or female/f)
- Invalid activity level value

### 6. Example Calculation

**Input:**
- Gender: Male
- Age: 30 years
- Height: 180 cm
- Weight: 75.5 kg
- Activity Level: Moderate

**Calculation:**
```
BMR Base = 66.5 + (13.75 × 75.5) + (5.003 × 180) - (6.75 × 30)
         = 66.5 + 1038.125 + 900.54 - 202.5
         = 1741.26 kcal/day

BMR with Activity = 1741.26 × 1.55
                  = 2698.96 kcal/day
```

### 7. API Usage Example

```bash
# 1. Create User
POST /user/{userId}
{
  "name": "John Doe",
  "email": "john@example.com"
}

# 2. Answer Questionnaire
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

# 3. Provide Start Values
POST /user/{userId}/start-values
{
  "startWeight": 75.5,
  "measurements": {
    "chest": 95.0,
    "waist": 85.0,
    "hips": 100.0
  }
}

# 4. Complete Onboarding (BMR calculated automatically)
POST /user/{userId}/complete-onboarding
# No request body needed

# 5. Fetch User (includes BMR values)
GET /user/{userId}
{
  "userId": "...",
  "name": "John Doe",
  "email": "john@example.com",
  "onboardingState": "Complete",
  "bmrBase": 1741.26,
  "bmrWithActivityLevel": 2698.96,
  ...
}
```

### 8. Testing

Run the included test script:
```bash
./test-bmr-api.sh
```

Or run unit tests:
```bash
dotnet test tests/tracker.App.Tests/tracker.App.Tests.csproj --filter "FullyQualifiedName~UserActorOnboardingSpecs"
```

### 9. Benefits of This Implementation

1. **Server-side Calculation**: BMR is calculated on the server, ensuring consistency and preventing client-side manipulation
2. **Single Source of Truth**: All BMR calculations use the same formulas
3. **Automatic**: No need for clients to implement BMR calculation logic
4. **Validated**: Input data is validated before calculation
5. **Persisted**: BMR values are stored in the event store and can be retrieved later
6. **Auditable**: The event journal contains all the data used for calculation

## Future Enhancements

Consider adding:
- Support for different BMR formulas (Harris-Benedict, Mifflin-St Jeor, etc.)
- Recalculation of BMR when weight changes
- BMR history tracking
- Custom activity level multipliers
- Support for more gender options with appropriate formulas

