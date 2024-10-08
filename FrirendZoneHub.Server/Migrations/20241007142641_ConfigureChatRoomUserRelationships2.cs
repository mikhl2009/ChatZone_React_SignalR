using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrirendZoneHub.Server.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureChatRoomUserRelationships2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_ChatRooms_ChatRoomId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_ChatRooms_ChatRoomId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ChatRoomId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ChatRoomId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ChatRoomId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ChatRoomId1",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "UserChatRoom",
                columns: table => new
                {
                    ChatRoomId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChatRoom", x => new { x.ChatRoomId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UserChatRoom_ChatRoomId",
                        column: x => x.ChatRoomId,
                        principalTable: "ChatRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserChatRoom_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserChatRoom_UserId",
                table: "UserChatRoom",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserChatRoom");

            migrationBuilder.AddColumn<int>(
                name: "ChatRoomId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChatRoomId1",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ChatRoomId",
                table: "Users",
                column: "ChatRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ChatRoomId1",
                table: "Users",
                column: "ChatRoomId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ChatRooms_ChatRoomId",
                table: "Users",
                column: "ChatRoomId",
                principalTable: "ChatRooms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_ChatRooms_ChatRoomId1",
                table: "Users",
                column: "ChatRoomId1",
                principalTable: "ChatRooms",
                principalColumn: "Id");
        }
    }
}
