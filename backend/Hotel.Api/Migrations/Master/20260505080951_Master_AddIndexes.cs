using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hotel.Api.Migrations.Master
{
    /// <inheritdoc />
    public partial class Master_AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CustomersGlobal_Email_IsVerified",
                table: "CustomersGlobal",
                columns: new[] { "Email", "IsVerified" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomersGlobal_Email_IsVerified",
                table: "CustomersGlobal");
        }
    }
}
