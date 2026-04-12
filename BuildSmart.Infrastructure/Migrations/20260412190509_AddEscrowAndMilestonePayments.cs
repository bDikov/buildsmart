using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEscrowAndMilestonePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobDescription",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RequestedDate",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ScheduledDate",
                table: "Bookings");

            migrationBuilder.AddColumn<Guid>(
                name: "BidId",
                table: "Bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "JobPostId",
                table: "Bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "MilestonePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountAllocated_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AmountAllocated_Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountAllocated_Tax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountAllocated_Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StripeTransferId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestonePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MilestonePayments_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MilestonePayments_JobTasks_JobTaskId",
                        column: x => x.JobTaskId,
                        principalTable: "JobTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BidId",
                table: "Bookings",
                column: "BidId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_JobPostId",
                table: "Bookings",
                column: "JobPostId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestonePayments_BookingId",
                table: "MilestonePayments",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestonePayments_JobTaskId",
                table: "MilestonePayments",
                column: "JobTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Bids_BidId",
                table: "Bookings",
                column: "BidId",
                principalTable: "Bids",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_JobPosts_JobPostId",
                table: "Bookings",
                column: "JobPostId",
                principalTable: "JobPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Bids_BidId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_JobPosts_JobPostId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "MilestonePayments");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BidId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_JobPostId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BidId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "JobPostId",
                table: "Bookings");

            migrationBuilder.AddColumn<string>(
                name: "JobDescription",
                table: "Bookings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedDate",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDate",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
