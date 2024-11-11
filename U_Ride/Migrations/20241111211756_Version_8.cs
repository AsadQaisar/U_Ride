using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U_Ride.Migrations
{
    /// <inheritdoc />
    public partial class Version_8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncodedPolyline",
                table: "Rides",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncodedPolyline",
                table: "Rides");
        }
    }
}
