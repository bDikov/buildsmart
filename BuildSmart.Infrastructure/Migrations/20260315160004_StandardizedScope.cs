using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StandardizedScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "JobTaskId",
                table: "JobPostQuestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EarliestStartDate",
                table: "Bids",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationDays",
                table: "Bids",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LatestStartDate",
                table: "Bids",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SequenceOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTasks_JobPosts_JobPostId",
                        column: x => x.JobPostId,
                        principalTable: "JobPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BidItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BidId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Price_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Price_Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Price_Tax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Price_Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BidItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BidItems_Bids_BidId",
                        column: x => x.BidId,
                        principalTable: "Bids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BidItems_JobTasks_JobTaskId",
                        column: x => x.JobTaskId,
                        principalTable: "JobTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskAcceptanceCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAcceptanceCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskAcceptanceCriteria_JobTasks_JobTaskId",
                        column: x => x.JobTaskId,
                        principalTable: "JobTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobPostQuestions_JobTaskId",
                table: "JobPostQuestions",
                column: "JobTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_BidItems_BidId",
                table: "BidItems",
                column: "BidId");

            migrationBuilder.CreateIndex(
                name: "IX_BidItems_JobTaskId",
                table: "BidItems",
                column: "JobTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTasks_JobPostId",
                table: "JobTasks",
                column: "JobPostId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAcceptanceCriteria_JobTaskId",
                table: "TaskAcceptanceCriteria",
                column: "JobTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobPostQuestions_JobTasks_JobTaskId",
                table: "JobPostQuestions",
                column: "JobTaskId",
                principalTable: "JobTasks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobPostQuestions_JobTasks_JobTaskId",
                table: "JobPostQuestions");

            migrationBuilder.DropTable(
                name: "BidItems");

            migrationBuilder.DropTable(
                name: "TaskAcceptanceCriteria");

            migrationBuilder.DropTable(
                name: "JobTasks");

            migrationBuilder.DropIndex(
                name: "IX_JobPostQuestions_JobTaskId",
                table: "JobPostQuestions");

            migrationBuilder.DropColumn(
                name: "JobTaskId",
                table: "JobPostQuestions");

            migrationBuilder.DropColumn(
                name: "EarliestStartDate",
                table: "Bids");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationDays",
                table: "Bids");

            migrationBuilder.DropColumn(
                name: "LatestStartDate",
                table: "Bids");
        }
    }
}
