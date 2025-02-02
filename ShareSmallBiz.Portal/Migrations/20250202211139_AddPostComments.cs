using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShareSmallBiz.Portal.Migrations
{
    /// <inheritdoc />
    public partial class AddPostComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Keywords_Posts_PostId",
                table: "Keywords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PostLikes",
                table: "PostLikes");

            migrationBuilder.DropIndex(
                name: "IX_PostLikes_PostId",
                table: "PostLikes");

            migrationBuilder.DropIndex(
                name: "IX_Keywords_PostId",
                table: "Keywords");

            migrationBuilder.DropColumn(
                name: "PostId",
                table: "Keywords");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "PostLikes",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PostLikes",
                table: "PostLikes",
                columns: new[] { "PostId", "UserId" });

            migrationBuilder.CreateTable(
                name: "KeywordPost",
                columns: table => new
                {
                    PostCategoriesId = table.Column<int>(type: "INTEGER", nullable: false),
                    PostsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeywordPost", x => new { x.PostCategoriesId, x.PostsId });
                    table.ForeignKey(
                        name: "FK_KeywordPost_Keywords_PostCategoriesId",
                        column: x => x.PostCategoriesId,
                        principalTable: "Keywords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KeywordPost_Posts_PostsId",
                        column: x => x.PostsId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PostId = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ParentPostId = table.Column<int>(type: "INTEGER", nullable: true),
                    PostId1 = table.Column<int>(type: "INTEGER", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedID = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedID = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostComments_Posts_ParentPostId",
                        column: x => x.ParentPostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PostComments_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostComments_Posts_PostId1",
                        column: x => x.PostId1,
                        principalTable: "Posts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PostCommentLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PostCommentId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedID = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedID = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostCommentLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostCommentLikes_AspNetUsers_CreatedID",
                        column: x => x.CreatedID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PostCommentLikes_PostComments_PostCommentId",
                        column: x => x.PostCommentId,
                        principalTable: "PostComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeywordPost_PostsId",
                table: "KeywordPost",
                column: "PostsId");

            migrationBuilder.CreateIndex(
                name: "IX_PostCommentLikes_CreatedID",
                table: "PostCommentLikes",
                column: "CreatedID");

            migrationBuilder.CreateIndex(
                name: "IX_PostCommentLikes_PostCommentId",
                table: "PostCommentLikes",
                column: "PostCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_ParentPostId",
                table: "PostComments",
                column: "ParentPostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_PostId",
                table: "PostComments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_PostId1",
                table: "PostComments",
                column: "PostId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeywordPost");

            migrationBuilder.DropTable(
                name: "PostCommentLikes");

            migrationBuilder.DropTable(
                name: "PostComments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PostLikes",
                table: "PostLikes");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "PostLikes",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "PostId",
                table: "Keywords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PostLikes",
                table: "PostLikes",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_PostLikes_PostId",
                table: "PostLikes",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Keywords_PostId",
                table: "Keywords",
                column: "PostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Keywords_Posts_PostId",
                table: "Keywords",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id");
        }
    }
}
