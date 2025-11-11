# Database Migration Quick Reference

## Quick Start

### Automatic Migration (Recommended)
Migrations run automatically when you start the application with PostgreSQL enabled:

```bash
dotnet run --project src/tracker.App
```

That's it! All pending migrations will be applied on startup.

## Manual Migration Management

### Create a New Migration
```bash
./migrate.sh create AddColumnToRecipes
```

This creates a new migration file with a timestamp: `YYYYMMDDHHmm_AddColumnToRecipes.cs`

### Example Migration Templates

#### Add a Column
```csharp
[Migration(202411111430)]
public class AddImageUrlToRecipes : Migration
{
    public override void Up()
    {
        Alter.Table("recipes")
            .AddColumn("image_url")
            .AsString(500)
            .Nullable();
    }

    public override void Down()
    {
        Delete.Column("image_url").FromTable("recipes");
    }
}
```

#### Add a New Table
```csharp
[Migration(202411111445)]
public class AddFavoriteRecipes : Migration
{
    public override void Up()
    {
        Create.Table("favorite_recipes")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("user_id").AsInt32()
                .ForeignKey("users", "user_id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("recipe_id").AsInt32()
                .ForeignKey("recipes", "recipe_id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("created_at").AsDateTime()
                .WithDefaultValue(SystemMethods.CurrentDateTime);
        
        Create.Index("idx_favorite_recipes_user")
            .OnTable("favorite_recipes")
            .OnColumn("user_id");
    }

    public override void Down()
    {
        Delete.Table("favorite_recipes");
    }
}
```

#### Add Data (Seed)
```csharp
[Migration(202411111500)]
public class SeedCommonIngredients : Migration
{
    public override void Up()
    {
        Insert.IntoTable("ingredients")
            .Row(new { 
                name = "Olive Oil", 
                calories_per_100g = 884, 
                protein_per_100g = 0, 
                fat_per_100g = 100, 
                carbs_per_100g = 0 
            })
            .Row(new { 
                name = "Garlic", 
                calories_per_100g = 149, 
                protein_per_100g = 6.4, 
                fat_per_100g = 0.5, 
                carbs_per_100g = 33 
            });
    }

    public override void Down()
    {
        Delete.FromTable("ingredients")
            .Row(new { name = "Olive Oil" })
            .Row(new { name = "Garlic" });
    }
}
```

#### Modify Column
```csharp
[Migration(202411111515)]
public class IncreaseRecipeTitleLength : Migration
{
    public override void Up()
    {
        Alter.Table("recipes")
            .AlterColumn("title")
            .AsString(500)
            .NotNullable();
    }

    public override void Down()
    {
        Alter.Table("recipes")
            .AlterColumn("title")
            .AsString(200)
            .NotNullable();
    }
}
```

#### Add Index
```csharp
[Migration(202411111530)]
public class AddRecipeSearchIndexes : Migration
{
    public override void Up()
    {
        Create.Index("idx_recipes_title")
            .OnTable("recipes")
            .OnColumn("title");
        
        Create.Index("idx_recipes_tags")
            .OnTable("recipes")
            .OnColumn("tags");
    }

    public override void Down()
    {
        Delete.Index("idx_recipes_title").OnTable("recipes");
        Delete.Index("idx_recipes_tags").OnTable("recipes");
    }
}
```

## Common FluentMigrator Methods

### Table Operations
```csharp
Create.Table("table_name")
Alter.Table("table_name")
Delete.Table("table_name")
Rename.Table("old_name").To("new_name")
```

### Column Types
```csharp
.AsInt32()           // INTEGER
.AsInt64()           // BIGINT
.AsString(length)    // VARCHAR(length)
.AsBoolean()         // BOOLEAN
.AsFloat()           // FLOAT
.AsDecimal(18, 2)    // DECIMAL(18,2)
.AsDateTime()        // TIMESTAMP
.AsDate()            // DATE
```

### Column Constraints
```csharp
.NotNullable()
.Nullable()
.PrimaryKey()
.Identity()          // Auto-increment
.Unique()
.Indexed()
.WithDefaultValue(value)
.ForeignKey("table", "column")
```

### Index Operations
```csharp
Create.Index("index_name")
    .OnTable("table_name")
    .OnColumn("column_name")
    .Ascending()
    .Descending()
```

### Foreign Keys
```csharp
Create.ForeignKey("FK_name")
    .FromTable("child_table").ForeignColumn("foreign_id")
    .ToTable("parent_table").PrimaryColumn("id")
    .OnDelete(Rule.Cascade)
    .OnUpdate(Rule.NoAction)
```

## Migration Helper Script

### Available Commands
```bash
# Run all pending migrations
./migrate.sh up

# Rollback last migration
./migrate.sh down

# Create new migration
./migrate.sh create MigrationName

# List all migrations
./migrate.sh list

# Migrate to specific version
./migrate.sh version 202411111430

# Show help
./migrate.sh help
```

## Database Schema After Initial Migration

```
recipes
├── recipe_id (PK)
├── title
├── description
├── total_calories
├── protein_g
├── fat_g
├── carbs_g
├── servings
├── prep_time_min
├── tags
└── instructions

ingredients
├── ingredient_id (PK)
├── name (UNIQUE)
├── calories_per_100g
├── protein_per_100g
├── fat_per_100g
└── carbs_per_100g

recipe_ingredients (junction)
├── recipe_id (PK, FK)
├── ingredient_id (PK, FK)
└── quantity_g

users
├── user_id (PK)
├── external_user_id (UNIQUE)
└── created_at

mealplans
├── mealplan_id (PK)
├── user_id (FK)
├── plan_name
├── start_date
├── end_date
├── total_calories
└── created_at

mealplan_days
├── day_id (PK)
├── mealplan_id (FK)
├── plan_date
└── day_total_calories

mealplan_meals
├── meal_id (PK)
├── day_id (FK)
├── meal_type (breakfast/lunch/dinner/snack)
└── calorie_target

mealplan_recipes (junction)
├── meal_id (PK, FK)
├── recipe_id (PK, FK)
└── servings
```

## Troubleshooting

### Migration Already Applied
If you try to run a migration that's already been applied, FluentMigrator will skip it automatically.

### Rollback Issues
Always test your `Down()` method. If rollback fails, you may need to manually fix the database.

### Check Migration Status
```sql
SELECT * FROM versioninfo ORDER BY version;
```

### Manual SQL Execution
If needed, you can execute raw SQL:
```csharp
Execute.Sql("ALTER TABLE ...");
```

### Clear All Migrations (DANGER!)
Only in development:
```sql
DELETE FROM versioninfo;
DROP TABLE recipes, ingredients, recipe_ingredients, 
           mealplans, mealplan_days, mealplan_meals, mealplan_recipes;
```

## Best Practices

✅ **DO:**
- Always implement both `Up()` and `Down()`
- Use descriptive migration names
- Test migrations on a copy of production data
- Keep migrations small and focused
- Add indexes for foreign keys
- Use transactions (automatic in FluentMigrator)

❌ **DON'T:**
- Edit migrations after they're deployed
- Delete old migrations
- Put business logic in migrations
- Forget to add indexes
- Skip testing rollbacks

## Configuration

### Connection String
Set in `appsettings.json` or environment variable:

```json
{
  "ConnectionStrings": {
    "PostgreSql": "Host=localhost;Database=tracker;Username=postgres;Password=yourpass"
  }
}
```

### Enable/Disable Auto-Migration
Auto-migration runs when `PersistenceMode` is set to `PostgreSql` in `AkkaSettings`.

## Status Check

After starting the app, verify migrations:

```bash
# Connect to PostgreSQL
psql -h localhost -U postgres -d tracker

# Check tables
\dt

# Check migration history
SELECT version, appliedon, description FROM versioninfo;
```

## Next Steps

1. ✅ Migrations are configured
2. ✅ Initial schema created
3. 🔄 Start your application
4. 🔄 Verify tables are created
5. 🎯 Start building features!

