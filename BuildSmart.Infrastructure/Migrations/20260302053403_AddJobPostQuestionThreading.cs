using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPostQuestionThreading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobPostQuestions_TradesmanProfiles_TradesmanProfileId",
                table: "JobPostQuestions");

            migrationBuilder.AlterColumn<string>(
                name: "QuestionText",
                table: "JobPostQuestions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AnswerText",
                table: "JobPostQuestions",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentQuestionId",
                table: "JobPostQuestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobPostQuestions_ParentQuestionId",
                table: "JobPostQuestions",
                column: "ParentQuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobPostQuestions_JobPostQuestions_ParentQuestionId",
                table: "JobPostQuestions",
                column: "ParentQuestionId",
                principalTable: "JobPostQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JobPostQuestions_TradesmanProfiles_TradesmanProfileId",
                table: "JobPostQuestions",
                column: "TradesmanProfileId",
                principalTable: "TradesmanProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobPostQuestions_JobPostQuestions_ParentQuestionId",
                table: "JobPostQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_JobPostQuestions_TradesmanProfiles_TradesmanProfileId",
                table: "JobPostQuestions");

            migrationBuilder.DropIndex(
                name: "IX_JobPostQuestions_ParentQuestionId",
                table: "JobPostQuestions");

            migrationBuilder.DropColumn(
                name: "ParentQuestionId",
                table: "JobPostQuestions");

            migrationBuilder.AlterColumn<string>(
                name: "QuestionText",
                table: "JobPostQuestions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "AnswerText",
                table: "JobPostQuestions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_JobPostQuestions_TradesmanProfiles_TradesmanProfileId",
                table: "JobPostQuestions",
                column: "TradesmanProfileId",
                principalTable: "TradesmanProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
