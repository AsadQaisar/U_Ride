using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U_Ride.Migrations
{
    /// <inheritdoc />
    public partial class Version_5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RideID",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    BookingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RideID = table.Column<int>(type: "int", nullable: false),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    BookingDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.BookingID);
                });

            migrationBuilder.CreateTable(
                name: "Rides",
                columns: table => new
                {
                    RideID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    StartPoint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EndPoint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<double>(type: "float", nullable: true),
                    AvailableSeats = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rides", x => x.RideID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_RideID",
                table: "Vehicles",
                column: "RideID");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Rides_RideID",
                table: "Vehicles",
                column: "RideID",
                principalTable: "Rides",
                principalColumn: "RideID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Rides_RideID",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Rides");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_RideID",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RideID",
                table: "Vehicles");
        }
    }
}
