using FluentMigrator;

namespace tracker.App.Migrations;

/// <summary>
/// Add scaling_factor column to mealplan_recipes table to support recipe scaling
/// </summary>
[Migration(20241125001)]
public class AddScalingFactorToMealPlanRecipes : Migration
{
    public override void Up()
    {
        Alter.Table("mealplan_recipes")
            .AddColumn("scaling_factor").AsDouble().WithDefaultValue(1.0).NotNullable();
    }

    public override void Down()
    {
        Delete.Column("scaling_factor").FromTable("mealplan_recipes");
    }
}

