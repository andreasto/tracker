using FluentMigrator;

namespace tracker.App.Migrations;

/// <summary>
/// Initial migration to create recipe, ingredient, and meal planning tables
/// </summary>
[Migration(20241111001)]
public class InitialRecipeAndMealPlanningTables : Migration
{
    public override void Up()
    {
        // =====================================================
        // RECIPES
        // =====================================================
        Create.Table("recipes")
            .WithColumn("recipe_id").AsInt32().PrimaryKey().Identity()
            .WithColumn("title").AsString(200).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable()
            .WithColumn("total_calories").AsFloat().NotNullable()
            .WithColumn("protein_g").AsFloat().Nullable()
            .WithColumn("fat_g").AsFloat().Nullable()
            .WithColumn("carbs_g").AsFloat().Nullable()
            .WithColumn("servings").AsInt32().WithDefaultValue(1)
            .WithColumn("prep_time_min").AsInt32().Nullable()
            .WithColumn("tags").AsString(200).Nullable()
            .WithColumn("instructions").AsString(int.MaxValue).Nullable();

        // =====================================================
        // INGREDIENTS
        // =====================================================
        Create.Table("ingredients")
            .WithColumn("ingredient_id").AsInt32().PrimaryKey().Identity()
            .WithColumn("name").AsString(100).NotNullable().Unique()
            .WithColumn("calories_per_100g").AsFloat().Nullable()
            .WithColumn("protein_per_100g").AsFloat().Nullable()
            .WithColumn("fat_per_100g").AsFloat().Nullable()
            .WithColumn("carbs_per_100g").AsFloat().Nullable();

        // =====================================================
        // RECIPE_INGREDIENTS (junction table)
        // =====================================================
        Create.Table("recipe_ingredients")
            .WithColumn("recipe_id").AsInt32().NotNullable()
                .ForeignKey("recipes", "recipe_id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("ingredient_id").AsInt32().NotNullable()
                .ForeignKey("ingredients", "ingredient_id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("quantity_g").AsFloat().NotNullable();

        Create.PrimaryKey("PK_recipe_ingredients")
            .OnTable("recipe_ingredients")
            .Columns("recipe_id", "ingredient_id");

        // =====================================================
        // MEALPLANS
        // Note: user_id is a string matching the Akka persistence_id format (e.g., "user-123")
        // No foreign key since users are stored in the event journal, not a traditional table
        // =====================================================
        Create.Table("mealplans")
            .WithColumn("mealplan_id").AsInt32().PrimaryKey().Identity()
            .WithColumn("user_id").AsString(255).NotNullable()
            .WithColumn("plan_name").AsString(100).NotNullable()
            .WithColumn("start_date").AsDate().NotNullable()
            .WithColumn("end_date").AsDate().Nullable()
            .WithColumn("total_calories").AsFloat().Nullable()
            .WithColumn("created_at").AsDateTime().WithDefaultValue(SystemMethods.CurrentDateTime);

        // =====================================================
        // MEALPLAN_DAYS
        // =====================================================
        Create.Table("mealplan_days")
            .WithColumn("day_id").AsInt32().PrimaryKey().Identity()
            .WithColumn("mealplan_id").AsInt32().NotNullable()
                .ForeignKey("mealplans", "mealplan_id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("plan_date").AsDate().NotNullable()
            .WithColumn("day_total_calories").AsFloat().Nullable();

        // =====================================================
        // MEALPLAN_MEALS
        // =====================================================
        Create.Table("mealplan_meals")
            .WithColumn("meal_id").AsInt32().PrimaryKey().Identity()
            .WithColumn("day_id").AsInt32().NotNullable()
                .ForeignKey("mealplan_days", "day_id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("meal_type").AsString(20).NotNullable()
            .WithColumn("calorie_target").AsFloat().Nullable();

        // Add check constraint for meal_type
        Execute.Sql(@"
            ALTER TABLE mealplan_meals 
            ADD CONSTRAINT chk_meal_type 
            CHECK (meal_type IN ('breakfast', 'lunch', 'dinner', 'snack'))
        ");

        // =====================================================
        // MEALPLAN_RECIPES (junction table)
        // =====================================================
        Create.Table("mealplan_recipes")
            .WithColumn("meal_id").AsInt32().NotNullable()
                .ForeignKey("mealplan_meals", "meal_id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("recipe_id").AsInt32().NotNullable()
                .ForeignKey("recipes", "recipe_id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("servings").AsFloat().WithDefaultValue(1);

        Create.PrimaryKey("PK_mealplan_recipes")
            .OnTable("mealplan_recipes")
            .Columns("meal_id", "recipe_id");

        // Create indexes for better query performance
        Create.Index("idx_mealplans_user_id").OnTable("mealplans").OnColumn("user_id");
        Create.Index("idx_mealplan_days_mealplan_id").OnTable("mealplan_days").OnColumn("mealplan_id");
        Create.Index("idx_mealplan_days_plan_date").OnTable("mealplan_days").OnColumn("plan_date");
        Create.Index("idx_mealplan_meals_day_id").OnTable("mealplan_meals").OnColumn("day_id");
        Create.Index("idx_recipe_ingredients_recipe_id").OnTable("recipe_ingredients").OnColumn("recipe_id");
        Create.Index("idx_recipe_ingredients_ingredient_id").OnTable("recipe_ingredients").OnColumn("ingredient_id");
    }

    public override void Down()
    {
        // Drop tables in reverse order (respecting foreign key constraints)
        Delete.Table("mealplan_recipes");
        Delete.Table("mealplan_meals");
        Delete.Table("mealplan_days");
        Delete.Table("mealplans");
        Delete.Table("recipe_ingredients");
        Delete.Table("ingredients");
        Delete.Table("recipes");
    }
}

