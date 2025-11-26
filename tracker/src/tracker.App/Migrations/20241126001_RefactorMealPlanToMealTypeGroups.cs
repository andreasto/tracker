using FluentMigrator;

namespace tracker.App.Migrations;

/// <summary>
/// Refactor meal plan structure from day-based to meal-type-based grouping
/// Remove mealplan_days table and update mealplan_meals to reference mealplan_id directly
/// </summary>
[Migration(20241126001)]
public class RefactorMealPlanToMealTypeGroups : Migration
{
    public override void Up()
    {
        // Step 1: Drop all existing meal plan data (simpler migration for structure change)
        Delete.Table("mealplan_recipes");
        Delete.Table("mealplan_meals");
        Delete.Table("mealplan_days");

        // Step 2: Remove date columns from mealplans table
        Delete.Column("start_date").FromTable("mealplans");
        Delete.Column("end_date").FromTable("mealplans");

        // Step 3: Create new mealplan_meals table with the new structure
        Create.Table("mealplan_meals")
            .WithColumn("meal_id").AsInt32().PrimaryKey().Identity()
            .WithColumn("mealplan_id").AsInt32().NotNullable()
                .ForeignKey("mealplans", "mealplan_id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("meal_type").AsString(20).NotNullable()
            .WithColumn("calorie_target").AsFloat().Nullable()
            .WithColumn("meal_order").AsInt32().NotNullable().WithDefaultValue(1);

        // Step 4: Add check constraint for meal_type
        Execute.Sql(@"
            ALTER TABLE mealplan_meals 
            ADD CONSTRAINT chk_meal_type 
            CHECK (meal_type IN ('breakfast', 'lunch', 'dinner', 'snack'))
        ");

        // Step 5: Recreate mealplan_recipes table
        Create.Table("mealplan_recipes")
            .WithColumn("meal_id").AsInt32().NotNullable()
                .ForeignKey("mealplan_meals", "meal_id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("recipe_id").AsInt32().NotNullable()
                .ForeignKey("recipes", "recipe_id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("servings").AsFloat().WithDefaultValue(1)
            .WithColumn("scaling_factor").AsDouble().WithDefaultValue(1.0).NotNullable();

        Create.PrimaryKey("PK_mealplan_recipes")
            .OnTable("mealplan_recipes")
            .Columns("meal_id", "recipe_id");

        // Step 6: Create indexes for better query performance
        Create.Index("idx_mealplan_meals_mealplan_id").OnTable("mealplan_meals").OnColumn("mealplan_id");
        Create.Index("idx_mealplan_meals_meal_type").OnTable("mealplan_meals").OnColumn("meal_type");
        Create.Index("idx_mealplan_meals_order").OnTable("mealplan_meals").OnColumn("meal_order");
    }

    public override void Down()
    {
        // Restore date columns
        Alter.Table("mealplans")
            .AddColumn("start_date").AsDate().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .AddColumn("end_date").AsDate().Nullable();

        // Recreate mealplan_days table
        Create.Table("mealplan_days")
            .WithColumn("day_id").AsInt32().PrimaryKey().Identity()
            .WithColumn("mealplan_id").AsInt32().NotNullable()
                .ForeignKey("mealplans", "mealplan_id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("plan_date").AsDate().NotNullable()
            .WithColumn("day_total_calories").AsFloat().Nullable();

        // Rename current table
        Rename.Table("mealplan_meals").To("mealplan_meals_old");

        // Recreate old mealplan_meals structure
        Create.Table("mealplan_meals")
            .WithColumn("meal_id").AsInt32().PrimaryKey().Identity()
            .WithColumn("day_id").AsInt32().NotNullable()
                .ForeignKey("mealplan_days", "day_id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("meal_type").AsString(20).NotNullable()
            .WithColumn("calorie_target").AsFloat().Nullable();

        Execute.Sql(@"
            ALTER TABLE mealplan_meals 
            ADD CONSTRAINT chk_meal_type 
            CHECK (meal_type IN ('breakfast', 'lunch', 'dinner', 'snack'))
        ");

        // Drop new structure
        Delete.Table("mealplan_meals_old");

        // Recreate indexes
        Create.Index("idx_mealplan_days_mealplan_id").OnTable("mealplan_days").OnColumn("mealplan_id");
        Create.Index("idx_mealplan_days_plan_date").OnTable("mealplan_days").OnColumn("plan_date");
        Create.Index("idx_mealplan_meals_day_id").OnTable("mealplan_meals").OnColumn("day_id");
    }
}

