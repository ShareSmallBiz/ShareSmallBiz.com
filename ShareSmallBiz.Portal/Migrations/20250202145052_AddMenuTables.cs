using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace ShareSmallBiz.Portal.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Posts",
                newName: "Published");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "UserFollows",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedID",
                table: "UserFollows",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "UserFollows",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ModifiedID",
                table: "UserFollows",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cover",
                table: "Posts",
                type: "TEXT",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Posts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedID",
                table: "Posts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Posts",
                type: "TEXT",
                maxLength: 450,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Posts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "Posts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ModifiedID",
                table: "Posts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostType",
                table: "Posts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PostViews",
                table: "Posts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Posts",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "Selected",
                table: "Posts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Posts",
                type: "TEXT",
                maxLength: 160,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "PostLikes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedID",
                table: "PostLikes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "PostLikes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ModifiedID",
                table: "PostLikes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContentParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedID = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentParts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Keywords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    PostId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedID = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Keywords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Keywords_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WebSites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Template = table.Column<string>(type: "TEXT", nullable: false),
                    GalleryFolder = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    DomainUrl = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    UseBreadCrumbUrl = table.Column<bool>(type: "INTEGER", nullable: false),
                    VersionNo = table.Column<int>(type: "INTEGER", nullable: false),
                    Style = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsRecipeSite = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedID = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebSites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentPartKeyword",
                columns: table => new
                {
                    ContentPartId = table.Column<int>(type: "INTEGER", nullable: false),
                    KeywordId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPartKeyword", x => new { x.ContentPartId, x.KeywordId });
                    table.ForeignKey(
                        name: "FK_ContentPartKeyword_ContentParts_ContentPartId",
                        column: x => x.ContentPartId,
                        principalTable: "ContentParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContentPartKeyword_Keywords_KeywordId",
                        column: x => x.KeywordId,
                        principalTable: "Keywords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    KeyWords = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Controller = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Argument = table.Column<string>(type: "TEXT", nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PageContent = table.Column<string>(type: "TEXT", nullable: false),
                    DomainId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedID = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Menu_Domain",
                        column: x => x.DomainId,
                        principalTable: "WebSites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Menu_ParentMenu_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Menus",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MenuKeyword",
                columns: table => new
                {
                    KeywordId = table.Column<int>(type: "INTEGER", nullable: false),
                    MenuId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuKeyword", x => new { x.KeywordId, x.MenuId });
                    table.ForeignKey(
                        name: "FK_MenuKeyword_Keywords_KeywordId",
                        column: x => x.KeywordId,
                        principalTable: "Keywords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuKeyword_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentPartKeyword_KeywordId",
                table: "ContentPartKeyword",
                column: "KeywordId");

            migrationBuilder.CreateIndex(
                name: "IX_Keywords_PostId",
                table: "Keywords",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuKeyword_MenuId",
                table: "MenuKeyword",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_DomainId",
                table: "Menus",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_ParentId",
                table: "Menus",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_WebSites_DomainUrl",
                table: "WebSites",
                column: "DomainUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebSites_Name",
                table: "WebSites",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebSites_Title",
                table: "WebSites",
                column: "Title",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentPartKeyword");

            migrationBuilder.DropTable(
                name: "MenuKeyword");

            migrationBuilder.DropTable(
                name: "ContentParts");

            migrationBuilder.DropTable(
                name: "Keywords");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DropTable(
                name: "WebSites");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "UserFollows");

            migrationBuilder.DropColumn(
                name: "CreatedID",
                table: "UserFollows");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "UserFollows");

            migrationBuilder.DropColumn(
                name: "ModifiedID",
                table: "UserFollows");

            migrationBuilder.DropColumn(
                name: "Cover",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CreatedID",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ModifiedID",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "PostType",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "PostViews",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Selected",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "PostLikes");

            migrationBuilder.DropColumn(
                name: "CreatedID",
                table: "PostLikes");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "PostLikes");

            migrationBuilder.DropColumn(
                name: "ModifiedID",
                table: "PostLikes");

            migrationBuilder.RenameColumn(
                name: "Published",
                table: "Posts",
                newName: "CreatedAt");
        }
    }
}
