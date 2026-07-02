using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectTranslationColumnsAndCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceCategoryTranslations");

            migrationBuilder.DropTable(
                name: "ServiceSkuTranslations");

            migrationBuilder.AlterColumn<string>(
                name: "UnitType",
                table: "ServiceSkus",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SkuCode",
                table: "ServiceSkus",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ServiceSkus",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ServiceSkus",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CalculationFormula",
                table: "ServiceSkus",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "BasePrice",
                table: "ServiceSkus",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "EnglishDescription",
                table: "ServiceSkus",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                table: "ServiceSkus",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnglishUnitType",
                table: "ServiceSkus",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnglishDescription",
                table: "ServiceCategories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                table: "ServiceCategories",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnglishHint",
                table: "Questions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnglishOptionsJson",
                table: "Questions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnglishText",
                table: "Questions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSkus_SkuCode",
                table: "ServiceSkus",
                column: "SkuCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceSkus_SkuCode",
                table: "ServiceSkus");

            migrationBuilder.DropColumn(
                name: "EnglishDescription",
                table: "ServiceSkus");

            migrationBuilder.DropColumn(
                name: "EnglishName",
                table: "ServiceSkus");

            migrationBuilder.DropColumn(
                name: "EnglishUnitType",
                table: "ServiceSkus");

            migrationBuilder.DropColumn(
                name: "EnglishDescription",
                table: "ServiceCategories");

            migrationBuilder.DropColumn(
                name: "EnglishName",
                table: "ServiceCategories");

            migrationBuilder.DropColumn(
                name: "EnglishHint",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "EnglishOptionsJson",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "EnglishText",
                table: "Questions");

            migrationBuilder.AlterColumn<string>(
                name: "UnitType",
                table: "ServiceSkus",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "SkuCode",
                table: "ServiceSkus",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ServiceSkus",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(250)",
                oldMaxLength: 250);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ServiceSkus",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "CalculationFormula",
                table: "ServiceSkus",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<decimal>(
                name: "BasePrice",
                table: "ServiceSkus",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.CreateTable(
                name: "ServiceCategoryTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    LanguageCode = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCategoryTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCategoryTranslations_ServiceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceSkuTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    LanguageCode = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UnitType = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSkuTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceSkuTranslations_ServiceSkus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "ServiceSkus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategoryTranslations_CategoryId",
                table: "ServiceCategoryTranslations",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSkuTranslations_SkuId",
                table: "ServiceSkuTranslations",
                column: "SkuId");
        }
    }
}
