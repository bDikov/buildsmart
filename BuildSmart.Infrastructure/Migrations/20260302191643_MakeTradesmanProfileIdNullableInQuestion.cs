using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeTradesmanProfileIdNullableInQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobPostQuestions_TradesmanProfiles_TradesmanProfileId",
                table: "JobPostQuestions");

            migrationBuilder.AlterColumn<Guid>(
                name: "TradesmanProfileId",
                table: "JobPostQuestions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AuthorId",
                table: "JobPostQuestions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobPostQuestions_AuthorId",
                table: "JobPostQuestions",
                column: "AuthorId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobPostQuestions_TradesmanProfiles_TradesmanProfileId",
                table: "JobPostQuestions",
                column: "TradesmanProfileId",
                principalTable: "TradesmanProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_JobPostQuestions_Users_AuthorId",
                table: "JobPostQuestions",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobPostQuestions_TradesmanProfiles_TradesmanProfileId",
                table: "JobPostQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_JobPostQuestions_Users_AuthorId",
                table: "JobPostQuestions");

            migrationBuilder.DropIndex(
                name: "IX_JobPostQuestions_AuthorId",
                table: "JobPostQuestions");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "JobPostQuestions");

            migrationBuilder.AlterColumn<Guid>(
                name: "TradesmanProfileId",
                table: "JobPostQuestions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_JobPostQuestions_TradesmanProfiles_TradesmanProfileId",
                table: "JobPostQuestions",
                column: "TradesmanProfileId",
                principalTable: "TradesmanProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
