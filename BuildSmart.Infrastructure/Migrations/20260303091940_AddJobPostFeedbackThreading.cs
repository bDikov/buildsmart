using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPostFeedbackThreading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobPostFeedbacks_Users_AuthorId",
                table: "JobPostFeedbacks");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentFeedbackId",
                table: "JobPostFeedbacks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobPostFeedbacks_ParentFeedbackId",
                table: "JobPostFeedbacks",
                column: "ParentFeedbackId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobPostFeedbacks_JobPostFeedbacks_ParentFeedbackId",
                table: "JobPostFeedbacks",
                column: "ParentFeedbackId",
                principalTable: "JobPostFeedbacks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobPostFeedbacks_Users_AuthorId",
                table: "JobPostFeedbacks",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobPostFeedbacks_JobPostFeedbacks_ParentFeedbackId",
                table: "JobPostFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_JobPostFeedbacks_Users_AuthorId",
                table: "JobPostFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_JobPostFeedbacks_ParentFeedbackId",
                table: "JobPostFeedbacks");

            migrationBuilder.DropColumn(
                name: "ParentFeedbackId",
                table: "JobPostFeedbacks");

            migrationBuilder.AddForeignKey(
                name: "FK_JobPostFeedbacks_Users_AuthorId",
                table: "JobPostFeedbacks",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
