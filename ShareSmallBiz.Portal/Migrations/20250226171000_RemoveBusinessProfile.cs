using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShareSmallBiz.Portal.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBusinessProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_BusinessProfiles_BusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_SocialLinks_BusinessProfiles_BusinessId",
                table: "SocialLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_Testimonials_BusinessProfiles_BusinessId",
                table: "Testimonials");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCollaborations_BusinessProfiles_BusinessId1",
                table: "UserCollaborations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCollaborations_BusinessProfiles_BusinessId2",
                table: "UserCollaborations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCollaborations_BusinessProfiles_BusinessProfileId",
                table: "UserCollaborations");

            migrationBuilder.DropForeignKey(
                name: "FK_UserContentContributions_BusinessProfiles_BusinessId",
                table: "UserContentContributions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserServices_BusinessProfiles_BusinessId",
                table: "UserServices");

            migrationBuilder.DropTable(
                name: "BusinessProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserServices_BusinessId",
                table: "UserServices");

            migrationBuilder.DropIndex(
                name: "IX_UserContentContributions_BusinessId",
                table: "UserContentContributions");

            migrationBuilder.DropIndex(
                name: "IX_UserCollaborations_BusinessId1",
                table: "UserCollaborations");

            migrationBuilder.DropIndex(
                name: "IX_UserCollaborations_BusinessId2",
                table: "UserCollaborations");

            migrationBuilder.DropIndex(
                name: "IX_UserCollaborations_BusinessProfileId",
                table: "UserCollaborations");

            migrationBuilder.DropIndex(
                name: "IX_Testimonials_BusinessId",
                table: "Testimonials");

            migrationBuilder.DropIndex(
                name: "IX_SocialLinks_BusinessId",
                table: "SocialLinks");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_BusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "UserServices");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "UserContentContributions");

            migrationBuilder.DropColumn(
                name: "BusinessId1",
                table: "UserCollaborations");

            migrationBuilder.DropColumn(
                name: "BusinessId2",
                table: "UserCollaborations");

            migrationBuilder.DropColumn(
                name: "BusinessProfileId",
                table: "UserCollaborations");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "Testimonials");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "SocialLinks");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsSoleProprietor",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OwnedBusinessId",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessId",
                table: "UserServices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessId",
                table: "UserContentContributions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessId1",
                table: "UserCollaborations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessId2",
                table: "UserCollaborations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessProfileId",
                table: "UserCollaborations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessId",
                table: "Testimonials",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessId",
                table: "SocialLinks",
                type: "TEXT",
                nullable: true);

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
                name: "OwnedBusinessId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessProfiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: false),
                    BusinessDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    BusinessEmail = table.Column<string>(type: "TEXT", nullable: false),
                    BusinessName = table.Column<string>(type: "TEXT", nullable: false),
                    BusinessPhone = table.Column<string>(type: "TEXT", nullable: false),
                    Industry = table.Column<string>(type: "TEXT", nullable: false),
                    Keywords = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    MetaDescription = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_UserServices_BusinessId",
                table: "UserServices",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_UserContentContributions_BusinessId",
                table: "UserContentContributions",
                column: "BusinessId");

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
                name: "IX_Testimonials_BusinessId",
                table: "Testimonials",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialLinks_BusinessId",
                table: "SocialLinks",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_BusinessId",
                table: "AspNetUsers",
                column: "BusinessId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_BusinessProfiles_BusinessId",
                table: "AspNetUsers",
                column: "BusinessId",
                principalTable: "BusinessProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SocialLinks_BusinessProfiles_BusinessId",
                table: "SocialLinks",
                column: "BusinessId",
                principalTable: "BusinessProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Testimonials_BusinessProfiles_BusinessId",
                table: "Testimonials",
                column: "BusinessId",
                principalTable: "BusinessProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCollaborations_BusinessProfiles_BusinessId1",
                table: "UserCollaborations",
                column: "BusinessId1",
                principalTable: "BusinessProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCollaborations_BusinessProfiles_BusinessId2",
                table: "UserCollaborations",
                column: "BusinessId2",
                principalTable: "BusinessProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCollaborations_BusinessProfiles_BusinessProfileId",
                table: "UserCollaborations",
                column: "BusinessProfileId",
                principalTable: "BusinessProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserContentContributions_BusinessProfiles_BusinessId",
                table: "UserContentContributions",
                column: "BusinessId",
                principalTable: "BusinessProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserServices_BusinessProfiles_BusinessId",
                table: "UserServices",
                column: "BusinessId",
                principalTable: "BusinessProfiles",
                principalColumn: "Id");
        }
    }
}
