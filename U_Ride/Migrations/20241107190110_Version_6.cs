using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U_Ride.Migrations
{
    /// <inheritdoc />
    public partial class Version_6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Rides",
                newName: "DriverID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DriverID",
                table: "Rides",
                newName: "UserID");
        }
    }
}
