using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBidItemJobTaskCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BidItems_JobTasks_JobTaskId",
                table: "BidItems");

            migrationBuilder.AddForeignKey(
                name: "FK_BidItems_JobTasks_JobTaskId",
                table: "BidItems",
                column: "JobTaskId",
                principalTable: "JobTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BidItems_JobTasks_JobTaskId",
                table: "BidItems");

            migrationBuilder.AddForeignKey(
                name: "FK_BidItems_JobTasks_JobTaskId",
                table: "BidItems",
                column: "JobTaskId",
                principalTable: "JobTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
