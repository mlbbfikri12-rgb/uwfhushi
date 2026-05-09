using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hotel.Api.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddBookingGroupsAndPendingExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BookingGroupId",
                table: "Bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmationEmailSentAtUtc",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAtUtc",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HoldUntilUtc",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "BookingGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupCode = table.Column<string>(type: "text", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    HoldUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingGroups_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingGroupId",
                table: "Bookings",
                column: "BookingGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status_HoldUntilUtc_RoomTypeId",
                table: "Bookings",
                columns: new[] { "Status", "HoldUntilUtc", "RoomTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingGroups_CustomerId",
                table: "BookingGroups",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingGroups_GroupCode",
                table: "BookingGroups",
                column: "GroupCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingGroups_Status_HoldUntilUtc",
                table: "BookingGroups",
                columns: new[] { "Status", "HoldUntilUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_BookingGroups_BookingGroupId",
                table: "Bookings",
                column: "BookingGroupId",
                principalTable: "BookingGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_BookingGroups_BookingGroupId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "BookingGroups");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookingGroupId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status_HoldUntilUtc_RoomTypeId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookingGroupId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ConfirmationEmailSentAtUtc",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ConfirmedAtUtc",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "HoldUntilUtc",
                table: "Bookings");
        }
    }
}
