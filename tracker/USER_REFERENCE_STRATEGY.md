# User Reference Strategy in Meal Planning Tables

## Problem

In an event-sourced architecture using Akka.Persistence, users don't exist as traditional database rows. Instead:
- User data is stored as events in the `journal` table
- Each user has a `persistence_id` like `"User_user-123"`
- User state is rebuilt by replaying events from the journal

This creates a challenge for meal planning tables that need to reference users.

## Solution: String-Based User References

The `mealplans` table uses a **string-based `user_id` column** instead of a traditional foreign key:

```sql
CREATE TABLE mealplans (
    mealplan_id SERIAL PRIMARY KEY,
    user_id VARCHAR(255) NOT NULL,  -- String, no foreign key!
    plan_name VARCHAR(100) NOT NULL,
    start_date DATE NOT NULL,
    ...
);
```

### Why This Works

1. **No Foreign Key Constraint**: The `user_id` is just a string identifier that matches the user's ID
2. **Application-Level Integrity**: Your application code ensures valid user IDs
3. **Flexible Architecture**: Works with event sourcing without creating circular dependencies

### User ID Format

The `user_id` should match whatever identifier you use for users in your system:
- Example: `"user-123"`, `"john@example.com"`, `"550e8400-e29b-41d4-a716-446655440000"`

## Schema Overview

```
┌────────────────────┐
│   Event Journal    │
│  (Akka Persist)    │
│                    │
│ persistence_id:    │
│  "User_user-123"   │
└─────────┬──────────┘
          │
          │ (no FK, just string reference)
          │
          ▼
┌─────────────────────┐
│     mealplans       │
├─────────────────────┤
│ user_id: "user-123" │ ◄── String reference
│ plan_name: ...      │
│ start_date: ...     │
└─────────────────────┘
```

## Benefits

✅ **Event Sourcing Friendly**: No tight coupling to traditional user tables
✅ **Flexible**: Can reference users regardless of where they're stored
✅ **Simple**: No complex join tables or materialized views needed
✅ **Performant**: Indexed string lookups are fast

## Trade-offs

⚠️ **No Database Integrity**: Database won't prevent invalid user IDs
- **Mitigation**: Validate user existence in application code before creating meal plans

⚠️ **Orphaned Records Possible**: If a user is deleted, their meal plans remain
- **Mitigation**: Implement cascade deletion logic in your UserActor

## Implementation Example

### Creating a Meal Plan

```csharp
public async Task<MealPlan> CreateMealPlan(string userId, string planName, DateTime startDate)
{
    // 1. Verify user exists (query the UserActor)
    var user = await _userActor.Ask<User>(new FetchUser(userId));
    if (user == null || !user.IsCreated)
    {
        throw new InvalidOperationException($"User {userId} does not exist");
    }
    
    // 2. Create meal plan with the user's ID
    await using var db = new DataConnection(ProviderName.PostgreSQL15, _connectionString);
    
    var mealPlanId = await db.InsertWithIdentityAsync(new MealPlan
    {
        UserId = userId,  // Just store the string ID
        PlanName = planName,
        StartDate = startDate
    });
    
    return await db.MealPlans.FirstAsync(m => m.MealPlanId == mealPlanId);
}
```

### Querying User's Meal Plans

```csharp
public async Task<List<MealPlan>> GetUserMealPlans(string userId)
{
    await using var db = new DataConnection(ProviderName.PostgreSQL15, _connectionString);
    
    return await db.GetTable<MealPlan>()
        .Where(m => m.UserId == userId)
        .ToListAsync();
}
```

### Handling User Deletion

In your `UserActor`, when processing a delete command:

```csharp
public class DeleteUserCommand : IUserCommand { ... }

// In UserActor's command handler
case DeleteUserCommand delete:
    // 1. Persist the user deletion event
    Persist(new UserDeleted(delete.UserId), evt => 
    {
        // 2. Clean up related data
        CleanupUserData(delete.UserId);
    });
    break;

private async Task CleanupUserData(string userId)
{
    await using var db = new DataConnection(ProviderName.PostgreSQL15, _connectionString);
    
    // Delete user's meal plans
    await db.GetTable<MealPlan>()
        .Where(m => m.UserId == userId)
        .DeleteAsync();
}
```

## Alternative Approaches (Not Recommended)

### ❌ Materialized User Table
Creating a separate `users` table that mirrors the event journal:
- **Problem**: Duplicate source of truth, sync issues
- **Complexity**: Need to maintain consistency between event journal and table

### ❌ Querying Event Journal Directly
Using foreign key to the `journal` table:
- **Problem**: Tight coupling to Akka persistence internals
- **Fragility**: Breaking changes if Akka schema changes

## Best Practices

1. ✅ **Always Validate**: Check user exists before creating meal plans
2. ✅ **Index the Column**: `user_id` column is indexed for fast lookups
3. ✅ **Consistent Format**: Use the same user ID format throughout your app
4. ✅ **Document It**: Make it clear this is an application-enforced relationship
5. ✅ **Handle Orphans**: Have a strategy for cleaning up orphaned records

## Migration Code

The migration correctly implements this strategy:

```csharp
Create.Table("mealplans")
    .WithColumn("mealplan_id").AsInt32().PrimaryKey().Identity()
    .WithColumn("user_id").AsString(255).NotNullable()  // String, not FK!
    .WithColumn("plan_name").AsString(100).NotNullable()
    // ... other columns

// Index for fast user lookups
Create.Index("idx_mealplans_user_id")
    .OnTable("mealplans")
    .OnColumn("user_id");
```

## Summary

This approach provides a **pragmatic solution** for referencing users in an event-sourced architecture:
- Simple and straightforward
- Works with Akka.Persistence
- Application enforces integrity
- Fast and indexed
- No coupling to Akka internals

It's the right choice for your architecture! 🎯

