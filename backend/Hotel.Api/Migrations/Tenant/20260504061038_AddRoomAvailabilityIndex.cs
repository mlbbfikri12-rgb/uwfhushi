using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hotel.Api.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddRoomAvailabilityIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "idx_room_availability_lookup",
                table: "RoomAvailabilities",
                columns: new[] { "RoomId", "Date", "IsAvailable" });

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Bookings_BookingId",
                table: "Payments",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoomAvailabilities_Rooms_RoomId",
                table: "RoomAvailabilities",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Bookings_BookingId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_RoomAvailabilities_Rooms_RoomId",
                table: "RoomAvailabilities");

            migrationBuilder.DropIndex(
                name: "idx_room_availability_lookup",
                table: "RoomAvailabilities");
        }
    }
}
