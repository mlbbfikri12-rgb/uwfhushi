using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hotel.Api.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class Tenant_AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoomTypeFacilities_RoomTypeId",
                table: "RoomTypeFacilities");

            migrationBuilder.DropIndex(
                name: "IX_Customers_GlobalCustomerId",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypes_Name",
                table: "RoomTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "idx_roomtype_facility_lookup",
                table: "RoomTypeFacilities",
                columns: new[] { "RoomTypeId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_GlobalCustomerId",
                table: "Customers",
                column: "GlobalCustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status_CheckIn",
                table: "Bookings",
                columns: new[] { "Status", "CheckIn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoomTypes_Name",
                table: "RoomTypes");

            migrationBuilder.DropIndex(
                name: "idx_roomtype_facility_lookup",
                table: "RoomTypeFacilities");

            migrationBuilder.DropIndex(
                name: "IX_Customers_GlobalCustomerId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status_CheckIn",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_RoomTypeFacilities_RoomTypeId",
                table: "RoomTypeFacilities",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_GlobalCustomerId",
                table: "Customers",
                column: "GlobalCustomerId");
        }
    }
}
