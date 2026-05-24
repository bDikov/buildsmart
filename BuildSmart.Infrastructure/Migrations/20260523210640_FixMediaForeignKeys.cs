using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMediaForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TradesmanMedia_TradesmanProfiles_TradesmanProfileId",
                table: "TradesmanMedia");

            migrationBuilder.DropIndex(
                name: "IX_TradesmanMedia_TradesmanProfileId",
                table: "TradesmanMedia");

            migrationBuilder.DropColumn(
                name: "TradesmanProfileId",
                table: "TradesmanMedia");

            migrationBuilder.DropColumn(
                name: "TradesmanId",
                table: "ProjectMilestoneMedia");

            migrationBuilder.CreateIndex(
                name: "IX_TradesmanMedia_TradesmanId",
                table: "TradesmanMedia",
                column: "TradesmanId");

            migrationBuilder.AddForeignKey(
                name: "FK_TradesmanMedia_TradesmanProfiles_TradesmanId",
                table: "TradesmanMedia",
                column: "TradesmanId",
                principalTable: "TradesmanProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TradesmanMedia_TradesmanProfiles_TradesmanId",
                table: "TradesmanMedia");

            migrationBuilder.DropIndex(
                name: "IX_TradesmanMedia_TradesmanId",
                table: "TradesmanMedia");

            migrationBuilder.AddColumn<Guid>(
                name: "TradesmanProfileId",
                table: "TradesmanMedia",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TradesmanId",
                table: "ProjectMilestoneMedia",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_TradesmanMedia_TradesmanProfileId",
                table: "TradesmanMedia",
                column: "TradesmanProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_TradesmanMedia_TradesmanProfiles_TradesmanProfileId",
                table: "TradesmanMedia",
                column: "TradesmanProfileId",
                principalTable: "TradesmanProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
