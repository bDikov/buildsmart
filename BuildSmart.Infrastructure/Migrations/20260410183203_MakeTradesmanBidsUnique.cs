using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeTradesmanBidsUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bids_JobPostId",
                table: "Bids");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_JobPostId_TradesmanProfileId",
                table: "Bids",
                columns: new[] { "JobPostId", "TradesmanProfileId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bids_JobPostId_TradesmanProfileId",
                table: "Bids");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_JobPostId",
                table: "Bids",
                column: "JobPostId");
        }
    }
}
