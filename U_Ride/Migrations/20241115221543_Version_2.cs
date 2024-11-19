using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U_Ride.Migrations
{
    /// <inheritdoc />
    public partial class Version_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Bookings_BookingID",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Rides_RideID",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_BookingID",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RideID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BookingID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RideID",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "DriverID",
                table: "Rides",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "StudentID",
                table: "Bookings",
                newName: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Rides_UserID",
                table: "Rides",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UserID",
                table: "Bookings",
                column: "UserID",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_UserID",
                table: "Bookings",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rides_Users_UserID",
                table: "Rides",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_UserID",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Rides_Users_UserID",
                table: "Rides");

            migrationBuilder.DropIndex(
                name: "IX_Rides_UserID",
                table: "Rides");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_UserID",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Rides",
                newName: "DriverID");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Bookings",
                newName: "StudentID");

            migrationBuilder.AddColumn<int>(
                name: "BookingID",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RideID",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_BookingID",
                table: "Users",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RideID",
                table: "Users",
                column: "RideID");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Bookings_BookingID",
                table: "Users",
                column: "BookingID",
                principalTable: "Bookings",
                principalColumn: "BookingID");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Rides_RideID",
                table: "Users",
                column: "RideID",
                principalTable: "Rides",
                principalColumn: "RideID");
        }
    }
}
