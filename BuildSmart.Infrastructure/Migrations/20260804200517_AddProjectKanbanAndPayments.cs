using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectKanbanAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "JobTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedTradesmanId",
                table: "JobPosts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoryTradesmanAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TradesmanId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedByAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryTradesmanAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryTradesmanAssignments_JobPosts_JobPostId",
                        column: x => x.JobPostId,
                        principalTable: "JobPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryTradesmanAssignments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryTradesmanAssignments_ServiceCategories_ServiceCateg~",
                        column: x => x.ServiceCategoryId,
                        principalTable: "ServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CategoryTradesmanAssignments_Users_AssignedByAdminId",
                        column: x => x.AssignedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CategoryTradesmanAssignments_Users_TradesmanId",
                        column: x => x.TradesmanId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsSystemNote = table.Column<bool>(type: "boolean", nullable: false),
                    ImageUrls = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskComments_JobTasks_JobTaskId",
                        column: x => x.JobTaskId,
                        principalTable: "JobTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskComments_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskPaymentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    FinalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidByAdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskPaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskPaymentRecords_JobTasks_JobTaskId",
                        column: x => x.JobTaskId,
                        principalTable: "JobTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskPaymentRecords_Users_PaidByAdminId",
                        column: x => x.PaidByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_AssignedTradesmanId",
                table: "JobPosts",
                column: "AssignedTradesmanId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTradesmanAssignments_AssignedByAdminId",
                table: "CategoryTradesmanAssignments",
                column: "AssignedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTradesmanAssignments_JobPostId",
                table: "CategoryTradesmanAssignments",
                column: "JobPostId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTradesmanAssignments_ProjectId",
                table: "CategoryTradesmanAssignments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTradesmanAssignments_ServiceCategoryId",
                table: "CategoryTradesmanAssignments",
                column: "ServiceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTradesmanAssignments_TradesmanId",
                table: "CategoryTradesmanAssignments",
                column: "TradesmanId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_AuthorId",
                table: "TaskComments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_JobTaskId",
                table: "TaskComments",
                column: "JobTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskPaymentRecords_JobTaskId",
                table: "TaskPaymentRecords",
                column: "JobTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskPaymentRecords_PaidByAdminId",
                table: "TaskPaymentRecords",
                column: "PaidByAdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobPosts_Users_AssignedTradesmanId",
                table: "JobPosts",
                column: "AssignedTradesmanId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobPosts_Users_AssignedTradesmanId",
                table: "JobPosts");

            migrationBuilder.DropTable(
                name: "CategoryTradesmanAssignments");

            migrationBuilder.DropTable(
                name: "TaskComments");

            migrationBuilder.DropTable(
                name: "TaskPaymentRecords");

            migrationBuilder.DropIndex(
                name: "IX_JobPosts_AssignedTradesmanId",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "JobTasks");

            migrationBuilder.DropColumn(
                name: "AssignedTradesmanId",
                table: "JobPosts");
        }
    }
}
