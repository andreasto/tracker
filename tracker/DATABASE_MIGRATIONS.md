# Database Migration System

## Overview
The tracker application now includes a robust database migration system using **FluentMigrator**. This allows you to version-control your database schema and apply changes incrementally.

## Features
- ✅ Automatic migration execution on application startup
- ✅ Version-controlled schema changes
- ✅ Rollback support
- ✅ PostgreSQL optimized
- ✅ Type-safe migrations using C#
- ✅ Works alongside Akka.Persistence tables

## Initial Migration

The first migration (version `20241111001`) creates the following tables:

### Tables Created

#### 1. **recipes**
Stores recipe information
- `recipe_id` (Primary Key, Auto-increment)
- `title` (VARCHAR(200), Required)
- `description` (TEXT)
- `total_calories` (FLOAT, Required)
- `protein_g` (FLOAT)
- `fat_g` (FLOAT)
- `carbs_g` (FLOAT)
- `servings` (INT, Default: 1)
- `prep_time_min` (INT)
- `tags` (VARCHAR(200))
- `instructions` (TEXT)

#### 2. **ingredients**
Stores ingredient nutritional information
- `ingredient_id` (Primary Key, Auto-increment)
- `name` (VARCHAR(100), Unique, Required)
- `calories_per_100g` (FLOAT)
- `protein_per_100g` (FLOAT)
- `fat_per_100g` (FLOAT)
- `carbs_per_100g` (FLOAT)

#### 3. **recipe_ingredients**
Junction table linking recipes to ingredients
- `recipe_id` (Foreign Key → recipes, CASCADE)
- `ingredient_id` (Foreign Key → ingredients, CASCADE)
- `quantity_g` (FLOAT, Required)
- Primary Key: (recipe_id, ingredient_id)

#### 4. **users**
User reference table (created only if doesn't exist)
- `user_id` (Primary Key, Auto-increment)
- `external_user_id` (VARCHAR(255), Unique)
- `created_at` (TIMESTAMP, Default: NOW)

**IMPORTANT NOTE**: In this event-sourced architecture, the `users` table is NOT created. Instead, users exist as events in the Akka.Persistence `journal` table. The `mealplans` table references users via a string-based `user_id` column (not a foreign key). This allows meal plans to work with event-sourced users without tight coupling. See `USER_REFERENCE_STRATEGY.md` for details.

#### 5. **mealplans**
Stores meal plans for users
- `mealplan_id` (Primary Key, Auto-increment)
- `user_id` (VARCHAR(255), NOT NULL - String reference to user, no FK)
- `plan_name` (VARCHAR(100), Required)
- `start_date` (DATE, Required)
- `end_date` (DATE)
- `total_calories` (FLOAT)
- `created_at` (TIMESTAMP, Default: NOW)

**Note**: `user_id` is a string identifier (e.g., "user-123") that matches the user's ID in the event-sourced system. There is no foreign key constraint since users are stored as events, not in a traditional table.

#### 6. **mealplan_days**
Individual days within a meal plan
- `day_id` (Primary Key, Auto-increment)
- `mealplan_id` (Foreign Key → mealplans, CASCADE)
- `plan_date` (DATE, Required)
- `day_total_calories` (FLOAT)

#### 7. **mealplan_meals**
Individual meals within a day
- `meal_id` (Primary Key, Auto-increment)
- `day_id` (Foreign Key → mealplan_days, CASCADE)
- `meal_type` (VARCHAR(20), CHECK: breakfast/lunch/dinner/snack)
- `calorie_target` (FLOAT)

#### 8. **mealplan_recipes**
Junction table linking meals to recipes
- `meal_id` (Foreign Key → mealplan_meals, CASCADE)
- `recipe_id` (Foreign Key → recipes, CASCADE)
- `servings` (FLOAT, Default: 1)
- Primary Key: (meal_id, recipe_id)

### Indexes
The migration creates the following indexes for query optimization:
- `idx_mealplans_user_id` on mealplans(user_id)
- `idx_mealplan_days_mealplan_id` on mealplan_days(mealplan_id)
- `idx_mealplan_days_plan_date` on mealplan_days(plan_date)
- `idx_mealplan_meals_day_id` on mealplan_meals(day_id)
- `idx_recipe_ingredients_recipe_id` on recipe_ingredients(recipe_id)
- `idx_recipe_ingredients_ingredient_id` on recipe_ingredients(ingredient_id)

## Configuration

### Packages Added
```xml
<PackageReference Include="FluentMigrator" Version="5.2.0" />
<PackageReference Include="FluentMigrator.Runner" Version="5.2.0" />
<PackageReference Include="FluentMigrator.Runner.Postgres" Version="5.2.0" />
```

### Program.cs Changes
The migration system is automatically configured when PostgreSQL persistence is enabled:

```csharp
// Migrations are registered in the service collection
builder.Services.AddDatabaseMigrations(connectionString);

// Migrations are executed on application startup
app.UseDatabaseMigrations();
```

## Usage

### Running Migrations
Migrations run automatically when the application starts if:
1. PostgreSQL persistence mode is enabled
2. A valid PostgreSQL connection string is configured

### Creating New Migrations

Create a new migration class in the `Migrations` folder:

```csharp
using FluentMigrator;

namespace tracker.App.Migrations;

[Migration(20241111002)]  // Use timestamp: YYYYMMDDnnn
public class AddNewFeature : Migration
{
    public override void Up()
    {
        Create.Table("new_table")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("name").AsString(100).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("new_table");
    }
}
```

**Migration Naming Convention:**
- Format: `YYYYMMDDnnn` where `nnn` is a sequence number (001, 002, etc.)
- Example: `20241111001` = November 11, 2024, migration #1

### Manual Migration Control

If you need to run migrations manually, you can inject `MigrationService`:

```csharp
// Run all pending migrations
migrationService.MigrateUp();

// Rollback last migration
migrationService.MigrateDown();

// Migrate to specific version
migrationService.MigrateTo(20241111001);

// List all migrations
migrationService.ListMigrations();
```

## Migration Best Practices

1. **Never Edit Existing Migrations**
   - Once a migration is deployed, create a new one to make changes
   - This ensures consistency across environments

2. **Always Implement Down()**
   - Provide a rollback path for every migration
   - Makes it possible to undo changes if needed

3. **Test Migrations**
   - Test both Up() and Down() methods
   - Verify on a development database before deploying

4. **Use Transactions**
   - FluentMigrator wraps each migration in a transaction by default
   - Ensures atomic operations

5. **Index Strategy**
   - Add indexes for foreign keys and frequently queried columns
   - Consider query patterns when designing indexes

## Migration Workflow

```
Development → Testing → Staging → Production

1. Create migration locally
2. Test migration (up and down)
3. Commit to version control
4. Deploy to testing environment
5. Verify migration success
6. Deploy to staging/production
```

## Troubleshooting

### Migration Fails on Startup
- Check PostgreSQL connection string
- Verify database exists and is accessible
- Check logs for specific error messages
- Ensure proper permissions for schema changes

### Rollback a Migration
```csharp
// In development, you can manually rollback
using var scope = app.Services.CreateScope();
var migrationService = scope.ServiceProvider.GetRequiredService<MigrationService>();
migrationService.MigrateDown();
```

### Skip Migrations on Startup
Set `PersistenceMode` to something other than `PostgreSql` in configuration, or remove the connection string.

## Database Schema Diagram

```
recipes ──┐
          ├── recipe_ingredients ──── ingredients
          │
          └── mealplan_recipes ──── mealplan_meals
                                          │
                                    mealplan_days
                                          │
                                      mealplans
                                          │
                                       users
```

## Connection String Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "PostgreSql": "Host=localhost;Database=tracker;Username=postgres;Password=yourpassword"
  },
  "AkkaSettings": {
    "PersistenceMode": "PostgreSql"
  }
}
```

### Environment Variables
```bash
export ConnectionStrings__PostgreSql="Host=localhost;Database=tracker;Username=postgres;Password=yourpassword"
```

## Verification

After the application starts, you can verify the tables were created:

```sql
-- List all tables
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY table_name;

-- Check migration history
SELECT * FROM public.versioninfo;
```

## FluentMigrator VersionInfo Table

FluentMigrator creates a `versioninfo` table to track applied migrations:

| Column | Type | Description |
|--------|------|-------------|
| Version | BIGINT | Migration version number |
| AppliedOn | TIMESTAMP | When the migration was applied |
| Description | VARCHAR | Migration description |

## Future Migration Examples

### Adding a Column
```csharp
[Migration(20241111002)]
public class AddRecipeCreatedDate : Migration
{
    public override void Up()
    {
        Alter.Table("recipes")
            .AddColumn("created_at")
            .AsDateTime()
            .WithDefaultValue(SystemMethods.CurrentDateTime);
    }

    public override void Down()
    {
        Delete.Column("created_at").FromTable("recipes");
    }
}
```

### Adding an Index
```csharp
[Migration(20241111003)]
public class AddRecipeTitleIndex : Migration
{
    public override void Up()
    {
        Create.Index("idx_recipes_title")
            .OnTable("recipes")
            .OnColumn("title");
    }

    public override void Down()
    {
        Delete.Index("idx_recipes_title").OnTable("recipes");
    }
}
```

### Data Migration
```csharp
[Migration(20241111004)]
public class SeedInitialIngredients : Migration
{
    public override void Up()
    {
        Insert.IntoTable("ingredients")
            .Row(new { 
                name = "Chicken Breast", 
                calories_per_100g = 165, 
                protein_per_100g = 31, 
                fat_per_100g = 3.6, 
                carbs_per_100g = 0 
            })
            .Row(new { 
                name = "Brown Rice", 
                calories_per_100g = 112, 
                protein_per_100g = 2.6, 
                fat_per_100g = 0.9, 
                carbs_per_100g = 24 
            });
    }

    public override void Down()
    {
        Delete.FromTable("ingredients")
            .Row(new { name = "Chicken Breast" })
            .Row(new { name = "Brown Rice" });
    }
}
```

## Status: ✅ READY TO USE

The migration system is fully configured and will run automatically on the next application startup.

