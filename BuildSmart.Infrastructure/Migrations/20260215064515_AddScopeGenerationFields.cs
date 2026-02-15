using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScopeGenerationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeneralSummary",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminFeedback",
                table: "JobPosts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneratedScope",
                table: "JobPosts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserEditedScope",
                table: "JobPosts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneralSummary",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AdminFeedback",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "GeneratedScope",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "UserEditedScope",
                table: "JobPosts");
        }
    }
}
