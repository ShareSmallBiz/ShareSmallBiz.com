using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShareSmallBiz.Portal.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomProfileUrl",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfileCompletenessScore",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProfileViewCount",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProfileVisibility",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomProfileUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileCompletenessScore",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileViewCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileVisibility",
                table: "AspNetUsers");
        }
    }
}
