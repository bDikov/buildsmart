using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCalculatorLeadsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalculatorLeads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Scope = table.Column<string>(type: "text", nullable: false),
                    SelectedArea = table.Column<int>(type: "integer", nullable: false),
                    BuildingStatus = table.Column<string>(type: "text", nullable: false),
                    QualityTier = table.Column<string>(type: "text", nullable: false),
                    IncludeFurniture = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeEquipment = table.Column<bool>(type: "boolean", nullable: false),
                    BathroomCount = table.Column<int>(type: "integer", nullable: false),
                    MinPriceEur = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxPriceEur = table.Column<decimal>(type: "numeric", nullable: false),
                    MinPriceBgn = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxPriceBgn = table.Column<decimal>(type: "numeric", nullable: false),
                    EstimatedDays = table.Column<int>(type: "integer", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationStatus = table.Column<string>(type: "text", nullable: false),
                    VerificationReason = table.Column<string>(type: "text", nullable: true),
                    UtmSource = table.Column<string>(type: "text", nullable: true),
                    UtmMedium = table.Column<string>(type: "text", nullable: true),
                    UtmCampaign = table.Column<string>(type: "text", nullable: true),
                    UtmTerm = table.Column<string>(type: "text", nullable: true),
                    UtmContent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculatorLeads", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalculatorLeads");
        }
    }
}
