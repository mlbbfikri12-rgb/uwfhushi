using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hotel.Api.Migrations.Master
{
    /// <inheritdoc />
    public partial class AddPgTrgmAndSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_hotels_name_trgm
                ON ""Hotels""
                USING gin (""Name"" gin_trgm_ops);
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_hotels_branchcode_trgm
                ON ""Hotels""
                USING gin (""BranchCode"" gin_trgm_ops);
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_cities_name_trgm
                ON ""Cities""
                USING gin (""Name"" gin_trgm_ops);
            ");

            // Relation-friendly indexes for common join/filter paths
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_hotels_cityid
                ON ""Hotels"" (""CityId"");
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_hotels_brandid
                ON ""Hotels"" (""BrandId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_hotels_name_trgm;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_hotels_branchcode_trgm;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_cities_name_trgm;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_hotels_cityid;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_hotels_brandid;");

        }
    }
}
