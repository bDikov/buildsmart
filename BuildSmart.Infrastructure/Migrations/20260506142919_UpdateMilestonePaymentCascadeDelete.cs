using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMilestonePaymentCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MilestonePayments_JobTasks_JobTaskId",
                table: "MilestonePayments");

            migrationBuilder.AddForeignKey(
                name: "FK_MilestonePayments_JobTasks_JobTaskId",
                table: "MilestonePayments",
                column: "JobTaskId",
                principalTable: "JobTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MilestonePayments_JobTasks_JobTaskId",
                table: "MilestonePayments");

            migrationBuilder.AddForeignKey(
                name: "FK_MilestonePayments_JobTasks_JobTaskId",
                table: "MilestonePayments",
                column: "JobTaskId",
                principalTable: "JobTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
