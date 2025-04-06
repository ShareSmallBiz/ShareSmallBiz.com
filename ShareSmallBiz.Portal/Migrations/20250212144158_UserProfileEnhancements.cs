using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace ShareSmallBiz.Portal.Migrations
{
    /// <inheritdoc />
    public partial class UserProfileEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Menu_Domain",
                table: "Menus");

            migrationBuilder.DropForeignKey(
                name: "FK_Menu_ParentMenu_ParentId",
                table: "Menus");

            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_Posts_ParentPostId",
                table: "PostComments");

            migrationBuilder.DropIndex(
                name: "IX_WebSites_DomainUrl",
                table: "WebSites");

            migrationBuilder.DropIndex(
                name: "IX_WebSites_Name",
                table: "WebSites");

            migrationBuilder.DropIndex(
                name: "IX_WebSites_Title",
                table: "WebSites");

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

            migrationBuilder.AddColumn<string>(
                name: "BusinessId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSoleProprietor",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Keywords",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 250,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "MetaDescription",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 160,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "OwnedBusinessId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PostLikes",
                table: "PostLikes",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "BusinessProfiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    BusinessName = table.Column<string>(type: "TEXT", nullable: false),
                    Industry = table.Column<string>(type: "TEXT", nullable: false),
                    BusinessDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    MetaDescription = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Keywords = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "TEXT", nullable: false),
                    BusinessEmail = table.Column<string>(type: "TEXT", nullable: false),
                    BusinessPhone = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessProfiles_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Platform = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialLinks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SocialLinks_BusinessProfiles_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Testimonials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    ReviewerName = table.Column<string>(type: "TEXT", nullable: false),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Testimonials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Testimonials_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Testimonials_BusinessProfiles_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserCollaborations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CollaborationTitle = table.Column<string>(type: "TEXT", nullable: false),
                    CollaborationDetails = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UserId1 = table.Column<string>(type: "TEXT", nullable: true),
                    UserId2 = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessId1 = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessId2 = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessProfileId = table.Column<string>(type: "TEXT", nullable: true),
                    ShareSmallBizUserId = table.Column<string>(type: "TEXT", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_UserCollaborations_BusinessProfiles_BusinessId1",
                        column: x => x.BusinessId1,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserCollaborations_BusinessProfiles_BusinessId2",
                        column: x => x.BusinessId2,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserCollaborations_BusinessProfiles_BusinessProfileId",
                        column: x => x.BusinessProfileId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserContentContributions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    ContentUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    DatePublished = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserContentContributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserContentContributions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserContentContributions_BusinessProfiles_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsBusinessService = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    BusinessId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserServices_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserServices_BusinessProfiles_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostLikes_PostId",
                table: "PostLikes",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_BusinessId",
                table: "AspNetUsers",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Slug",
                table: "AspNetUsers",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessProfiles_OwnerId",
                table: "BusinessProfiles",
                column: "OwnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessProfiles_Slug",
                table: "BusinessProfiles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocialLinks_BusinessId",
                table: "SocialLinks",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialLinks_UserId",
                table: "SocialLinks",
                column: "CreatedID");

            migrationBuilder.CreateIndex(
                name: "IX_Testimonials_BusinessId",
                table: "Testimonials",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Testimonials_UserId",
                table: "Testimonials",
                column: "CreatedID");

            migrationBuilder.CreateIndex(
                name: "IX_UserCollaborations_BusinessId1",
                table: "UserCollaborations",
                column: "BusinessId1");

            migrationBuilder.CreateIndex(
                name: "IX_UserCollaborations_BusinessId2",
                table: "UserCollaborations",
                column: "BusinessId2");

            migrationBuilder.CreateIndex(
                name: "IX_UserCollaborations_BusinessProfileId",
                table: "UserCollaborations",
                column: "BusinessProfileId");

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
                name: "IX_UserContentContributions_BusinessId",
                table: "UserContentContributions",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_UserContentContributions_UserId",
                table: "UserContentContributions",
                column: "CreatedID");

            migrationBuilder.CreateIndex(
                name: "IX_UserServices_BusinessId",
                table: "UserServices",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_UserServices_UserId",
                table: "UserServices",
                column: "CreatedID");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_BusinessProfiles_BusinessId",
                table: "AspNetUsers",
                column: "BusinessId",
                principalTable: "BusinessProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Menus_Menus_ParentId",
                table: "Menus",
                column: "ParentId",
                principalTable: "Menus",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Menus_WebSites_DomainId",
                table: "Menus",
                column: "DomainId",
                principalTable: "WebSites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_Posts_ParentPostId",
                table: "PostComments",
                column: "ParentPostId",
                principalTable: "Posts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_BusinessProfiles_BusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Menus_Menus_ParentId",
                table: "Menus");

            migrationBuilder.DropForeignKey(
                name: "FK_Menus_WebSites_DomainId",
                table: "Menus");

            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_Posts_ParentPostId",
                table: "PostComments");

            migrationBuilder.DropTable(
                name: "SocialLinks");

            migrationBuilder.DropTable(
                name: "Testimonials");

            migrationBuilder.DropTable(
                name: "UserCollaborations");

            migrationBuilder.DropTable(
                name: "UserContentContributions");

            migrationBuilder.DropTable(
                name: "UserServices");

            migrationBuilder.DropTable(
                name: "BusinessProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PostLikes",
                table: "PostLikes");

            migrationBuilder.DropIndex(
                name: "IX_PostLikes_PostId",
                table: "PostLikes");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_BusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Slug",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsSoleProprietor",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Keywords",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MetaDescription",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OwnedBusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "AspNetUsers");

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
                columns: new[] { "PostId", "CreatedID" });

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

            migrationBuilder.AddForeignKey(
                name: "FK_Menu_Domain",
                table: "Menus",
                column: "DomainId",
                principalTable: "WebSites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Menu_ParentMenu_ParentId",
                table: "Menus",
                column: "ParentId",
                principalTable: "Menus",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_Posts_ParentPostId",
                table: "PostComments",
                column: "ParentPostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
