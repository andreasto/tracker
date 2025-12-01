using FluentMigrator;

namespace tracker.App.Migrations;

[Migration(20241201001)]
public class AddEnhancedIngredientFields : Migration
{
    public override void Up()
    {
        // Add new columns to ingredients table for enhanced product data
        Alter.Table("ingredients")
            .AddColumn("external_product_id").AsString(255).Nullable()
            .AddColumn("ean").AsString(50).Nullable()
            .AddColumn("manufacturer").AsString(255).Nullable()
            .AddColumn("image_url").AsString(500).Nullable()
            .AddColumn("saturated_fat_per_100g").AsDouble().Nullable()
            .AddColumn("sugar_per_100g").AsDouble().Nullable()
            .AddColumn("salt_per_100g").AsDouble().Nullable()
            .AddColumn("fiber_per_100g").AsDouble().Nullable()
            .AddColumn("lowest_price").AsDouble().Nullable()
            .AddColumn("lowest_price_store").AsString(255).Nullable()
            .AddColumn("last_price_update").AsDateTime().Nullable()
            .AddColumn("last_synced_at").AsDateTime().Nullable();

        // Create index on EAN for faster lookups
        Create.Index("idx_ingredients_ean")
            .OnTable("ingredients")
            .OnColumn("ean");

        // Create index on external_product_id
        Create.Index("idx_ingredients_external_product_id")
            .OnTable("ingredients")
            .OnColumn("external_product_id");
    }

    public override void Down()
    {
        Delete.Index("idx_ingredients_ean").OnTable("ingredients");
        Delete.Index("idx_ingredients_external_product_id").OnTable("ingredients");

        Delete.Column("external_product_id").FromTable("ingredients");
        Delete.Column("ean").FromTable("ingredients");
        Delete.Column("manufacturer").FromTable("ingredients");
        Delete.Column("image_url").FromTable("ingredients");
        Delete.Column("saturated_fat_per_100g").FromTable("ingredients");
        Delete.Column("sugar_per_100g").FromTable("ingredients");
        Delete.Column("salt_per_100g").FromTable("ingredients");
        Delete.Column("fiber_per_100g").FromTable("ingredients");
        Delete.Column("lowest_price").FromTable("ingredients");
        Delete.Column("lowest_price_store").FromTable("ingredients");
        Delete.Column("last_price_update").FromTable("ingredients");
        Delete.Column("last_synced_at").FromTable("ingredients");
    }
}

