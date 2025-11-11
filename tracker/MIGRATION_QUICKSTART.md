# 🚀 Database Migration - Quick Start Guide

## TL;DR

Database migrations are now configured! When you start your app with PostgreSQL, the recipe and meal planning tables will be created automatically.

## Start Your Application

```bash
cd /Users/andreastornqvist/Development/tracker/tracker
dotnet run --project src/tracker.App
```

That's it! Migrations run automatically on startup. ✨

## What Happens on Startup

1. ✅ Connects to PostgreSQL
2. ✅ Checks for pending migrations
3. ✅ Applies migration `20241111001_InitialRecipeAndMealPlanningTables`
4. ✅ Creates 8 tables with indexes and constraints
5. ✅ Application starts normally

## Tables Created

- `recipes` - Store your recipes
- `ingredients` - Ingredient nutritional database
- `recipe_ingredients` - Link recipes to ingredients
- `users` - User reference
- `mealplans` - User meal plans
- `mealplan_days` - Days in meal plans
- `mealplan_meals` - Individual meals
- `mealplan_recipes` - Link meals to recipes

## Verify It Worked

### Check Logs
Look for this in your console:
```
[INF] Starting database migrations...
[INF] Database migrations completed successfully
```

### Check Database
```bash
psql -h localhost -U postgres -d tracker

# List tables
\dt

# Check migration history
SELECT * FROM versioninfo;
```

You should see all 8 tables created!

## Create Your First Migration

```bash
# Use the helper script
./migrate.sh create AddRecipeCategories
```

This creates a new migration file. Edit it to add your changes:

```csharp
[Migration(202411111545)]
public class AddRecipeCategories : Migration
{
    public override void Up()
    {
        Alter.Table("recipes")
            .AddColumn("category")
            .AsString(50)
            .Nullable();
    }

    public override void Down()
    {
        Delete.Column("category").FromTable("recipes");
    }
}
```

Next time you start the app, this migration runs automatically!

## Helper Commands

```bash
# Create new migration
./migrate.sh create MigrationName

# Show help
./migrate.sh help
```

## Configuration

Your `appsettings.json` should have:

```json
{
  "ConnectionStrings": {
    "PostgreSql": "Host=localhost;Database=tracker;Username=postgres;Password=yourpass"
  },
  "AkkaSettings": {
    "PersistenceMode": "PostgreSql"
  }
}
```

## Troubleshooting

### Migrations Don't Run
- ✅ Check PostgreSQL connection string
- ✅ Verify database exists: `createdb tracker`
- ✅ Check `PersistenceMode` is set to `PostgreSql`

### Build Errors
```bash
# Restore packages
dotnet restore

# Rebuild
dotnet build
```

### Database Connection Failed
```bash
# Test connection
psql -h localhost -U postgres -d tracker -c "SELECT 1;"

# Create database if needed
createdb tracker
```

## Documentation

- 📘 **DATABASE_MIGRATIONS.md** - Complete guide with examples
- 📗 **MIGRATION_QUICK_REFERENCE.md** - Quick reference and templates
- 📕 **MIGRATION_IMPLEMENTATION_SUMMARY.md** - Implementation details

## What's Next?

1. ✅ Start your application
2. ✅ Verify tables are created
3. 🎯 Build API endpoints for recipes
4. 🎯 Create meal planning features
5. 🎯 Add controllers and services

**You're ready to build! Happy coding! 🚀**

