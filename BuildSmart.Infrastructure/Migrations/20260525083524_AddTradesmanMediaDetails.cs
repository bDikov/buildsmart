using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTradesmanMediaDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "TradesmanMedia",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceCategoryId",
                table: "TradesmanMedia",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "TradesmanMedia",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TradesmanMedia_ServiceCategoryId",
                table: "TradesmanMedia",
                column: "ServiceCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_TradesmanMedia_ServiceCategories_ServiceCategoryId",
                table: "TradesmanMedia",
                column: "ServiceCategoryId",
                principalTable: "ServiceCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TradesmanMedia_ServiceCategories_ServiceCategoryId",
                table: "TradesmanMedia");

            migrationBuilder.DropIndex(
                name: "IX_TradesmanMedia_ServiceCategoryId",
                table: "TradesmanMedia");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "TradesmanMedia");

            migrationBuilder.DropColumn(
                name: "ServiceCategoryId",
                table: "TradesmanMedia");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "TradesmanMedia");
        }
    }
}
