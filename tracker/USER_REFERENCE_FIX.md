# ✅ FIXED: User Reference in Meal Planning Tables

## The Problem You Identified

The original migration tried to create a `users` table and link `mealplans` to it with a foreign key. But in your event-sourced architecture, users don't exist in a traditional table - they're stored as events in the Akka.Persistence `journal` table.

## The Solution

Changed `mealplans.user_id` from an integer foreign key to a **string-based reference**:

### Before (❌ Wrong)
```sql
CREATE TABLE users (
    user_id SERIAL PRIMARY KEY,
    ...
);

CREATE TABLE mealplans (
    user_id INT REFERENCES users(user_id) ON DELETE CASCADE,
    ...
);
```

### After (✅ Correct)
```sql
-- No users table created

CREATE TABLE mealplans (
    user_id VARCHAR(255) NOT NULL,  -- Just a string, no FK!
    ...
);

CREATE INDEX idx_mealplans_user_id ON mealplans(user_id);
```

## How It Works

```
Akka Event Journal                     Meal Planning Tables
─────────────────────                  ────────────────────

journal table:                         mealplans table:
┌─────────────────────┐               ┌──────────────────┐
│ persistence_id      │               │ user_id          │
│ "User_user-123"     │               │ "user-123"       │
│                     │  ─────────────│                  │
│ (events stored      │   string ref  │ plan_name        │
│  as JSON/binary)    │   (no FK!)    │ start_date       │
└─────────────────────┘               └──────────────────┘
```

## Benefits

✅ **Event Sourcing Compatible**: No coupling to traditional user tables
✅ **Flexible**: User ID can be any string format you choose
✅ **Simple**: No complex materialized views needed
✅ **Indexed**: Fast lookups via `idx_mealplans_user_id`

## What You Need to Do

### 1. Validate User Exists (Application Layer)

When creating a meal plan, check the user exists first:

```csharp
public async Task<int> CreateMealPlan(string userId, string planName, DateTime startDate)
{
    // Validate user exists by querying UserActor
    var user = await _userActor.Ask<User>(new FetchUser(userId));
    if (!user.IsCreated)
    {
        throw new InvalidOperationException($"User {userId} not found");
    }
    
    // Now create the meal plan
    await using var db = new DataConnection(ProviderName.PostgreSQL15, _connectionString);
    return await db.InsertWithIdentityAsync(new MealPlan 
    { 
        UserId = userId,  // Just store the string
        PlanName = planName,
        StartDate = startDate 
    });
}
```

### 2. Use Consistent User IDs

Make sure your user IDs are consistent:
- If you use `"user-123"` for the UserActor's persistence ID
- Store `"user-123"` in `mealplans.user_id`
- Don't include the `"User_"` prefix in the mealplans table

### 3. Handle User Deletion

When a user is deleted, clean up their meal plans:

```csharp
// In UserActor when processing delete
private async Task CleanupUserMealPlans(string userId)
{
    await using var db = new DataConnection(ProviderName.PostgreSQL15, _connectionString);
    await db.GetTable<MealPlan>()
        .Where(m => m.UserId == userId)
        .DeleteAsync();
}
```

## Files Updated

✅ **Migration File**: Removed users table, changed user_id to VARCHAR(255)
✅ **DATABASE_MIGRATIONS.md**: Updated documentation
✅ **USER_REFERENCE_STRATEGY.md**: New detailed guide created

## Database Schema (Final)

```
recipes                  ┐
  ├── recipe_id (PK)     │
  ├── title              │
  └── ...                │
                         ├─── recipe_ingredients
ingredients              │      ├── recipe_id (FK)
  ├── ingredient_id (PK) │      └── ingredient_id (FK)
  ├── name               │
  └── ...                ┘

mealplans                      ┐
  ├── mealplan_id (PK)         │
  ├── user_id (STRING) ◄────── │ No FK, just string!
  ├── plan_name                │
  └── ...                      │
                               │
mealplan_days                  │
  ├── day_id (PK)              │
  ├── mealplan_id (FK) ────────┤
  └── ...                      │
                               │
mealplan_meals                 │
  ├── meal_id (PK)             │
  ├── day_id (FK) ─────────────┤
  └── ...                      │
                               │
mealplan_recipes               │
  ├── meal_id (FK) ────────────┤
  └── recipe_id (FK) ──────────┘
```

## Summary

✅ **Problem Identified**: Original design tried to use FK to non-existent users table
✅ **Problem Fixed**: Changed to string-based reference compatible with event sourcing
✅ **Build Status**: ✅ Compiles successfully
✅ **Documentation**: ✅ Updated with detailed guide
✅ **Ready to Use**: ✅ Migration will work correctly with your Akka.Persistence setup

**Your meal planning tables are now correctly aligned with your event-sourced architecture!** 🎉

