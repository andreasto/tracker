# User Onboarding Flow

## Overview

The UserActor now implements a state machine pattern using Akka.NET's `Become` behavior to manage the user onboarding process. This ensures that users complete registration steps in the correct order.

## Onboarding States

The user progresses through the following states:

1. **NotStarted** - Initial state before user creation
2. **Registered** - User account created, awaiting questionnaire
3. **QuestionnaireAnswered** - Questionnaire completed, awaiting start measurements
4. **StartValuesProvided** - Measurements provided, awaiting onboarding completion
5. **Complete** - Fully onboarded and ready to use

## State Transitions

```
NotStarted 
    └─> CreateUser ──> Registered
                           └─> AnswerQuestionnaire ──> QuestionnaireAnswered
                                                            └─> ProvideStartValues ──> StartValuesProvided
                                                                                           └─> CompleteOnboarding ──> Complete
```

## API Endpoints

### 1. Create User
```http
POST /user/{userId}
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com"
}
```

**Effect**: Transitions from `NotStarted` → `Registered`

**Event**: `UserCreated`

---

### 2. Answer Questionnaire
```http
POST /user/{userId}/questionnaire
Content-Type: application/json

{
  "answers": {
    "activityLevel": "moderate",
    "goals": "weight-loss",
    "dietaryRestrictions": "none"
  }
}
```

**Requires**: User in `Registered` state

**Effect**: Transitions from `Registered` → `QuestionnaireAnswered`

**Event**: `QuestionnaireAnswered`

---

### 3. Provide Start Values
```http
POST /user/{userId}/start-values
Content-Type: application/json

{
  "startWeight": 75.5,
  "measurements": {
    "chest": 95.0,
    "waist": 85.0,
    "hips": 100.0,
    "arms": 30.0,
    "thighs": 55.0
  }
}
```

**Requires**: User in `QuestionnaireAnswered` state

**Effect**: Transitions from `QuestionnaireAnswered` → `StartValuesProvided`

**Event**: `StartValuesProvided`

---

### 4. Complete Onboarding
```http
POST /user/{userId}/complete-onboarding
```

**Requires**: User in `StartValuesProvided` state

**Effect**: Transitions from `StartValuesProvided` → `Complete`

**Event**: `UserOnboardingCompleted`

---

## State-Specific Behaviors

### In `Registered` State:
- ✅ Can answer questionnaire
- ✅ Can update name/email
- ❌ Cannot provide start values
- ❌ Cannot complete onboarding

### In `QuestionnaireAnswered` State:
- ✅ Can provide start values
- ✅ Can update name/email
- ❌ Cannot answer questionnaire again
- ❌ Cannot complete onboarding yet

### In `StartValuesProvided` State:
- ✅ Can complete onboarding
- ✅ Can update name/email
- ❌ Cannot answer questionnaire
- ❌ Cannot provide start values again

### In `Complete` State:
- ✅ Can update name/email
- ✅ Full system access
- ❌ Cannot repeat onboarding steps

## Example Flow

```bash
# 1. Create a user
curl -X POST http://localhost:5000/user/user-123 \
  -H "Content-Type: application/json" \
  -d '{"name":"John Doe","email":"john@example.com"}'

# 2. Answer questionnaire
curl -X POST http://localhost:5000/user/user-123/questionnaire \
  -H "Content-Type: application/json" \
  -d '{
    "answers": {
      "activityLevel": "moderate",
      "goals": "weight-loss"
    }
  }'

# 3. Provide start measurements
curl -X POST http://localhost:5000/user/user-123/start-values \
  -H "Content-Type: application/json" \
  -d '{
    "startWeight": 75.5,
    "measurements": {
      "chest": 95.0,
      "waist": 85.0
    }
  }'

# 4. Complete onboarding
curl -X POST http://localhost:5000/user/user-123/complete-onboarding

# 5. Check user state
curl http://localhost:5000/user/user-123
```

## Error Handling

If you try to execute a command in the wrong state, you'll receive an error response:

```json
{
  "userId": "user-123",
  "isSuccess": false,
  "errorMessage": "Cannot provide start values in state: Registered"
}
```

## Implementation Details

### Actor Behavior Switching

The UserActor uses Akka.NET's `Become` to switch behaviors based on state:

- `Become(RegisteredBehavior)` - After user creation
- `Become(QuestionnaireAnsweredBehavior)` - After questionnaire
- `Become(StartValuesProvidedBehavior)` - After start values
- `Become(CompleteBehavior)` - After onboarding completion

### Persistence

All state transitions are persisted as events:
- `UserCreated`
- `QuestionnaireAnswered`
- `StartValuesProvided`
- `UserOnboardingCompleted`

When the actor recovers from persistence, it automatically transitions to the correct behavior based on the last persisted state.

### Event Sourcing

The user state is rebuilt by applying events in sequence:
1. User data is loaded from snapshot or empty state
2. Events are replayed: `UserCreated` → `QuestionnaireAnswered` → etc.
3. After recovery, the actor switches to the appropriate behavior
4. New commands trigger new events, which update state and may trigger behavior changes

## Benefits

1. **Type Safety**: Commands are only accepted in valid states
2. **Clear Flow**: Explicit state machine makes onboarding process obvious
3. **Persistence**: State survives actor restarts
4. **Testability**: Each state's behavior can be tested independently
5. **Maintainability**: Easy to add new states or modify transitions

