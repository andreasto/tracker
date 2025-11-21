using FluentMigrator;

namespace tracker.App.Migrations;

[Migration(20241121001)]
public class CreateEmailLookupTable : Migration
{
    public override void Up()
    {
        Create.Table("email_lookup")
            .WithColumn("email").AsString(255).PrimaryKey().NotNullable()
            .WithColumn("user_id").AsString(100).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("idx_email_lookup_user_id")
            .OnTable("email_lookup")
            .OnColumn("user_id");
    }

    public override void Down()
    {
        Delete.Table("email_lookup");
    }
}

