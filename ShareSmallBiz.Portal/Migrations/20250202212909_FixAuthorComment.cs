using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShareSmallBiz.Portal.Migrations
{
    /// <inheritdoc />
    public partial class FixAuthorComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorId",
                table: "PostComments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_AuthorId",
                table: "PostComments",
                column: "AuthorId");

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_AspNetUsers_AuthorId",
                table: "PostComments",
                column: "AuthorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_AspNetUsers_AuthorId",
                table: "PostComments");

            migrationBuilder.DropIndex(
                name: "IX_PostComments_AuthorId",
                table: "PostComments");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "PostComments");
        }
    }
}
