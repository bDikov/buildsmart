using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAiCalculationSkuItemCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiCalculationSkuItems_ServiceSkus_ServiceSkuId",
                table: "AiCalculationSkuItems");

            migrationBuilder.AddForeignKey(
                name: "FK_AiCalculationSkuItems_ServiceSkus_ServiceSkuId",
                table: "AiCalculationSkuItems",
                column: "ServiceSkuId",
                principalTable: "ServiceSkus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiCalculationSkuItems_ServiceSkus_ServiceSkuId",
                table: "AiCalculationSkuItems");

            migrationBuilder.AddForeignKey(
                name: "FK_AiCalculationSkuItems_ServiceSkus_ServiceSkuId",
                table: "AiCalculationSkuItems",
                column: "ServiceSkuId",
                principalTable: "ServiceSkus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
