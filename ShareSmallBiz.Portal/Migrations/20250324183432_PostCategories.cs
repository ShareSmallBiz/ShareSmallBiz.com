using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShareSmallBiz.Portal.Migrations
{
    /// <inheritdoc />
    public partial class PostCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KeywordPost_Keywords_PostCategoriesId",
                table: "KeywordPost");

            migrationBuilder.DropForeignKey(
                name: "FK_KeywordPost_Posts_PostsId",
                table: "KeywordPost");

            migrationBuilder.DropPrimaryKey(
                name: "PK_KeywordPost",
                table: "KeywordPost");

            migrationBuilder.RenameTable(
                name: "KeywordPost",
                newName: "PostKeywords");

            migrationBuilder.RenameIndex(
                name: "IX_KeywordPost_PostsId",
                table: "PostKeywords",
                newName: "IX_PostKeywords_PostsId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PostKeywords",
                table: "PostKeywords",
                columns: new[] { "PostCategoriesId", "PostsId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PostKeywords_Keywords_PostCategoriesId",
                table: "PostKeywords",
                column: "PostCategoriesId",
                principalTable: "Keywords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostKeywords_Posts_PostsId",
                table: "PostKeywords",
                column: "PostsId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostKeywords_Keywords_PostCategoriesId",
                table: "PostKeywords");

            migrationBuilder.DropForeignKey(
                name: "FK_PostKeywords_Posts_PostsId",
                table: "PostKeywords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PostKeywords",
                table: "PostKeywords");

            migrationBuilder.RenameTable(
                name: "PostKeywords",
                newName: "KeywordPost");

            migrationBuilder.RenameColumn(
                name: "StorageProvider",
                table: "Media",
                newName: "StorageProviderNames");

            migrationBuilder.RenameIndex(
                name: "IX_PostKeywords_PostsId",
                table: "KeywordPost",
                newName: "IX_KeywordPost_PostsId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_KeywordPost",
                table: "KeywordPost",
                columns: new[] { "PostCategoriesId", "PostsId" });

            migrationBuilder.AddForeignKey(
                name: "FK_KeywordPost_Keywords_PostCategoriesId",
                table: "KeywordPost",
                column: "PostCategoriesId",
                principalTable: "Keywords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KeywordPost_Posts_PostsId",
                table: "KeywordPost",
                column: "PostsId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
