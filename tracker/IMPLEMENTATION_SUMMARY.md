# User Onboarding State Machine Implementation - Summary

## What Was Implemented

I've successfully implemented a state machine pattern for the UserActor using Akka.NET's `Become` behavior to manage the user onboarding process. Here's what was done:

## Changes Made

### 1. Domain Layer (`tracker.Domain/User/`)

#### **UserEvents.cs** - New Events
- `QuestionnaireAnswered` - Records user questionnaire responses
- `StartValuesProvided` - Records initial weight and measurements
- `UserOnboardingCompleted` - Marks onboarding as complete

#### **UserCommands.cs** - New Commands
- `AnswerQuestionnaireCommand` - Submit questionnaire answers
- `ProvideStartValuesCommand` - Submit start weight and measurements
- `CompleteOnboardingCommand` - Complete the onboarding process

### 2. Application Layer (`tracker.App/Actors/`)

#### **UserActor.cs** - Major Refactoring
- **Enhanced User Record**: Added onboarding state tracking
  ```csharp
  UserOnboardingState OnboardingState
  Dictionary<string, string>? QuestionnaireAnswers
  double? StartWeight
  Dictionary<string, double>? StartMeasurements
  ```

- **State Machine with 5 States**:
  1. `NotStarted` - Initial state
  2. `Registered` - After user creation
  3. `QuestionnaireAnswered` - After answering questions
  4. `StartValuesProvided` - After providing measurements
  5. `Complete` - Fully onboarded

- **Behavior Switching**: Each state has its own behavior that only accepts valid commands
  - `NotStartedBehavior()` - Only accepts CreateUser
  - `RegisteredBehavior()` - Accepts questionnaire, rejects others
  - `QuestionnaireAnsweredBehavior()` - Accepts start values
  - `StartValuesProvidedBehavior()` - Accepts completion
  - `CompleteBehavior()` - Full access

- **State Validation**: Commands are rejected with clear error messages if executed in wrong state

### 3. API Layer (`tracker.App/Controllers/`)

#### **UserController.cs** - New Endpoints

```http
POST /user/{userId}/questionnaire
POST /user/{userId}/start-values
POST /user/{userId}/complete-onboarding
```

### 4. Tests (`tracker.App.Tests/`)

#### **UserActorOnboardingSpecs.cs** - Comprehensive Test Suite
- ✅ `UserActor_should_follow_onboarding_flow` - Tests complete flow
- ✅ `UserActor_should_reject_questionnaire_after_completion` - Tests state validation
- ✅ `UserActor_should_allow_name_updates_during_onboarding` - Tests non-state-changing updates

**All 7 tests pass** (4 existing + 3 new)

## Key Features

### 1. **Type-Safe State Transitions**
Using Akka.NET's `Become`, the actor switches behavior based on state, ensuring type-safe transitions.

### 2. **Clear Error Messages**
If a user tries to skip a step, they get a clear error:
```
"Please complete the questionnaire first"
"Cannot provide start values in state: Registered"
```

### 3. **Event Sourcing**
All state changes are persisted as events, allowing:
- Full audit trail
- Recovery after actor restarts
- Time-travel debugging

### 4. **Persistence**
When the actor recovers from persistence, it:
1. Loads snapshot (if available)
2. Replays all events
3. Automatically switches to the correct behavior based on state

### 5. **Flexibility**
Users can still update their name/email during onboarding without affecting the onboarding state.

## Example Usage

```bash
# 1. Create user
curl -X POST http://localhost:5000/user/user-123 \
  -H "Content-Type: application/json" \
  -d '{"name":"John Doe","email":"john@example.com"}'

# 2. Answer questionnaire
curl -X POST http://localhost:5000/user/user-123/questionnaire \
  -H "Content-Type: application/json" \
  -d '{"answers":{"activityLevel":"moderate","goals":"weight-loss"}}'

# 3. Provide measurements
curl -X POST http://localhost:5000/user/user-123/start-values \
  -H "Content-Type: application/json" \
  -d '{"startWeight":75.5,"measurements":{"chest":95.0,"waist":85.0}}'

# 4. Complete onboarding
curl -X POST http://localhost:5000/user/user-123/complete-onboarding
```

## Documentation

Created comprehensive documentation:
- **USER_ONBOARDING_FLOW.md** - Complete guide with API examples and state diagrams

## Benefits

1. ✅ **Enforced Workflow**: Users must complete steps in order
2. ✅ **Clear State Management**: No ambiguity about what state a user is in
3. ✅ **Testable**: Each state behavior can be tested independently
4. ✅ **Maintainable**: Easy to add new states or modify transitions
5. ✅ **Resilient**: State survives actor restarts via event sourcing
6. ✅ **Type-Safe**: Compile-time checking for message types
7. ✅ **Self-Documenting**: The code clearly shows the onboarding flow

## Test Results

```
✅ All 7 tests passing
   - 4 existing tests (Counter, CheckIn)
   - 3 new onboarding tests
```

The implementation is production-ready and follows best practices for Akka.NET persistent actors with state machines.

