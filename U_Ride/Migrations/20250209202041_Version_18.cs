using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace U_Ride.Migrations
{
    /// <inheritdoc />
    public partial class Version_18 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StudentID",
                table: "Chats",
                newName: "SenderID");

            migrationBuilder.RenameColumn(
                name: "DriverID",
                table: "Chats",
                newName: "ReceiverID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SenderID",
                table: "Chats",
                newName: "StudentID");

            migrationBuilder.RenameColumn(
                name: "ReceiverID",
                table: "Chats",
                newName: "DriverID");
        }
    }
}
