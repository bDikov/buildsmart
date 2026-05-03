using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiCalculationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiCalculations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalEstimatedPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiCalculations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiCalculationTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AiCalculationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SequenceOrder = table.Column<int>(type: "integer", nullable: false),
                    EstimatedPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiCalculationTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiCalculationTasks_AiCalculations_AiCalculationId",
                        column: x => x.AiCalculationId,
                        principalTable: "AiCalculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiCalculationCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AiCalculationTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiCalculationCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiCalculationCriteria_AiCalculationTasks_AiCalculationTaskId",
                        column: x => x.AiCalculationTaskId,
                        principalTable: "AiCalculationTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiCalculationSkuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AiCalculationTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceSkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EstimatedPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiCalculationSkuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiCalculationSkuItems_AiCalculationTasks_AiCalculationTaskId",
                        column: x => x.AiCalculationTaskId,
                        principalTable: "AiCalculationTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiCalculationSkuItems_ServiceSkus_ServiceSkuId",
                        column: x => x.ServiceSkuId,
                        principalTable: "ServiceSkus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiCalculationCriteria_AiCalculationTaskId",
                table: "AiCalculationCriteria",
                column: "AiCalculationTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AiCalculations_ProjectId_ServiceCategoryId",
                table: "AiCalculations",
                columns: new[] { "ProjectId", "ServiceCategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiCalculationSkuItems_AiCalculationTaskId",
                table: "AiCalculationSkuItems",
                column: "AiCalculationTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AiCalculationSkuItems_ServiceSkuId",
                table: "AiCalculationSkuItems",
                column: "ServiceSkuId");

            migrationBuilder.CreateIndex(
                name: "IX_AiCalculationTasks_AiCalculationId",
                table: "AiCalculationTasks",
                column: "AiCalculationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiCalculationCriteria");

            migrationBuilder.DropTable(
                name: "AiCalculationSkuItems");

            migrationBuilder.DropTable(
                name: "AiCalculationTasks");

            migrationBuilder.DropTable(
                name: "AiCalculations");
        }
    }
}
