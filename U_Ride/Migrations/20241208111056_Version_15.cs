using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U_Ride.Migrations
{
    /// <inheritdoc />
    public partial class Version_15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SocketID",
                table: "Rides");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SocketID",
                table: "Rides",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
