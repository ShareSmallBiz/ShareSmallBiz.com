using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace ShareSmallBiz.Portal.Migrations
{
    /// <inheritdoc />
    public partial class UserToUserPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TargetId",
                table: "Posts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Posts_TargetId",
                table: "Posts",
                column: "TargetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_AspNetUsers_TargetId",
                table: "Posts",
                column: "TargetId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_AspNetUsers_TargetId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_TargetId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "TargetId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "AspNetUsers");
        }
    }
}
