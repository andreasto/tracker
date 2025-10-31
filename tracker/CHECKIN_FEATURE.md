# Weekly Check-In Feature

## Overview

The weekly check-in feature allows users to track their fitness progress by submitting weekly measurements. This feature is implemented using a separate `CheckInActor` following the actor model pattern used throughout the application.

## Architecture

### Domain Model (`tracker.Domain.CheckIn`)

The check-in domain is organized into:

- **Commands** (`CheckInCommands.cs`): Actions that modify check-in state
  - `SetWeeklyCheckInDayCommand` - Set the preferred day of the week for check-ins
  - `SubmitWeeklyCheckInCommand` - Submit a new weekly check-in with measurements
  
- **Events** (`CheckInEvents.cs`): Immutable facts about what happened
  - `WeeklyCheckInDaySet` - Records when a user sets their check-in day preference
  - `WeeklyCheckInSubmitted` - Records a submitted check-in with full measurements

- **Queries** (`CheckInQueries.cs`): Read-only operations
  - `FetchCheckIns` - Retrieve all check-in data for a user

### Actor Model (`tracker.App.Actors.CheckInActor`)

The `CheckInActor` is a persistent actor that:
- Manages check-in state per user (identified by userId)
- Stores check-in history with event sourcing
- Takes snapshots every 25 events for efficient recovery
- Persists to the configured persistence backend (InMemory, Azure, or PostgreSQL)

### State Model

```csharp
public record CheckInState(
    string UserId,
    DayOfWeek? WeeklyCheckInDay = null,
    List<WeeklyCheckIn>? CheckInHistory = null)

public record WeeklyCheckIn(DateTime SubmittedAt, Measurements Measurements)

public record Measurements(
    double Weight,
    double Thigh,
    double Glutes,
    double Hips,
    double Waist,
    double Stomach,
    double Chest,
    double Overarm)
```

## API Endpoints

### Set Weekly Check-In Day
```
PUT /checkin/{userId}/checkin-day
Content-Type: application/json

{
  "checkInDay": "Monday"  // Sunday, Monday, Tuesday, etc.
}
```

Response:
```json
{
  "userId": "user123",
  "checkInDay": "Monday"
}
```

### Submit Weekly Check-In
```
POST /checkin/{userId}
Content-Type: application/json

{
  "weight": 75.5,
  "thigh": 60.0,
  "glutes": 95.0,
  "hips": 100.0,
  "waist": 80.0,
  "stomach": 85.0,
  "chest": 95.0,
  "overarm": 35.0
}
```

Response:
```json
{
  "userId": "user123",
  "submittedAt": "2025-10-31T09:50:30Z",
  "weight": 75.5,
  "thigh": 60.0,
  "glutes": 95.0,
  "hips": 100.0,
  "waist": 80.0,
  "stomach": 85.0,
  "chest": 95.0,
  "overarm": 35.0
}
```

### Get Check-In History
```
GET /checkin/{userId}
```

Response:
```json
{
  "userId": "user123",
  "weeklyCheckInDay": "Monday",
  "checkInHistory": [
    {
      "submittedAt": "2025-10-31T09:50:30Z",
      "measurements": {
        "weight": 75.5,
        "thigh": 60.0,
        "glutes": 95.0,
        "hips": 100.0,
        "waist": 80.0,
        "stomach": 85.0,
        "chest": 95.0,
        "overarm": 35.0
      }
    }
  ]
}
```

## Design Decisions

### Why a Separate CheckInActor?

The check-in functionality was implemented as a separate actor rather than being part of the `UserActor` for several reasons:

1. **Single Responsibility Principle**: `UserActor` manages user identity and profile, while `CheckInActor` manages measurement tracking
2. **Scalability**: Check-in history grows over time (weekly data), separating it prevents the User actor from becoming bloated
3. **Performance**: Lighter snapshots for User actor, faster recovery times
4. **Consistency**: Follows the existing pattern where Counter is separate from User
5. **Independent Lifecycle**: Check-ins can be queried, analyzed, and managed independently of user profile data

### Measurements Tracked

The following measurements are tracked per check-in:
- **Weight** (in kg or lbs depending on user preference)
- **Thigh** (circumference in cm or inches)
- **Glutes** (circumference in cm or inches)
- **Hips** (circumference in cm or inches)
- **Waist** (circumference in cm or inches)
- **Stomach** (circumference in cm or inches)
- **Chest** (circumference in cm or inches)
- **Overarm** (circumference in cm or inches)

## Testing

Tests are available in `tracker.App.Tests/CheckInActorSpecs.cs`:
- Setting weekly check-in day
- Submitting weekly check-ins
- Storing and retrieving check-in history

Run tests with:
```bash
dotnet test
```

## Future Enhancements

Potential improvements:
- Add validation for measurement values (min/max ranges)
- Support for photos with check-ins
- Progress tracking and analytics
- Reminders for weekly check-ins on the selected day
- Export check-in history to CSV/Excel
- Comparison views (week-over-week, month-over-month)

