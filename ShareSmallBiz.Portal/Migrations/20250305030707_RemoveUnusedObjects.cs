using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace ShareSmallBiz.Portal.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Testimonials");

            migrationBuilder.DropTable(
                name: "UserCollaborations");

            migrationBuilder.DropTable(
                name: "UserContentContributions");

            migrationBuilder.DropTable(
                name: "UserServices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Testimonials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReviewerName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Testimonials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Testimonials_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserCollaborations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId1 = table.Column<string>(type: "TEXT", nullable: true),
                    UserId2 = table.Column<string>(type: "TEXT", nullable: true),
                    CollaborationDetails = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CollaborationTitle = table.Column<string>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ShareSmallBizUserId = table.Column<string>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCollaborations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCollaborations_AspNetUsers_ShareSmallBizUserId",
                        column: x => x.ShareSmallBizUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserCollaborations_AspNetUsers_UserId1",
                        column: x => x.UserId1,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserCollaborations_AspNetUsers_UserId2",
                        column: x => x.UserId2,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserContentContributions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    ContentUrl = table.Column<string>(type: "TEXT", nullable: false),
                    DatePublished = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserContentContributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserContentContributions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsBusinessService = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserServices_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Testimonials_UserId",
                table: "Testimonials",
                column: "CreatedID");

            migrationBuilder.CreateIndex(
                name: "IX_UserCollaborations_ShareSmallBizUserId",
                table: "UserCollaborations",
                column: "ShareSmallBizUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCollaborations_UserId1",
                table: "UserCollaborations",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_UserCollaborations_UserId2",
                table: "UserCollaborations",
                column: "UserId2");

            migrationBuilder.CreateIndex(
                name: "IX_UserContentContributions_UserId",
                table: "UserContentContributions",
                column: "CreatedID");

            migrationBuilder.CreateIndex(
                name: "IX_UserServices_UserId",
                table: "UserServices",
                column: "CreatedID");
        }
    }
}
