using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDualPricingMarkup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdminMarkupPercentage",
                table: "Projects",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 20.0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TradesmanPrice",
                table: "JobTasks",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminMarkupPercentage",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TradesmanPrice",
                table: "JobTasks");
        }
    }
}
