using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShareSmallBiz.Portal.Migrations
{
    /// <inheritdoc />
    public partial class FixPostComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_Posts_PostId1",
                table: "PostComments");

            migrationBuilder.DropIndex(
                name: "IX_PostComments_PostId1",
                table: "PostComments");

            migrationBuilder.DropColumn(
                name: "PostId1",
                table: "PostComments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PostId1",
                table: "PostComments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_PostId1",
                table: "PostComments",
                column: "PostId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_Posts_PostId1",
                table: "PostComments",
                column: "PostId1",
                principalTable: "Posts",
                principalColumn: "Id");
        }
    }
}
