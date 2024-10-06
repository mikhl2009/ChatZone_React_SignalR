using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrirendZoneHub.Server.Migrations
{
    /// <inheritdoc />
    public partial class models : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChatRoomId1",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ChatRoomId1",
                table: "Users",
                column: "ChatRoomId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ChatRooms_ChatRoomId1",
                table: "Users",
                column: "ChatRoomId1",
                principalTable: "ChatRooms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_ChatRooms_ChatRoomId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ChatRoomId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ChatRoomId1",
                table: "Users");
        }
    }
}
