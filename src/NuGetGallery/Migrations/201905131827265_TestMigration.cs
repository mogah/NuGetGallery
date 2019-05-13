namespace NuGetGallery.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TestMigration : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Packages", "TestMigration", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Packages", "TestMigration");
        }
    }
}
