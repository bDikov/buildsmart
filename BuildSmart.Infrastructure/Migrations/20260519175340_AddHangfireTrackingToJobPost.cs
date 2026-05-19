using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHangfireTrackingToJobPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveHangfireJobId",
                table: "JobPosts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastQueuedJobDetails",
                table: "JobPosts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveHangfireJobId",
                table: "JobPosts");

            migrationBuilder.DropColumn(
                name: "LastQueuedJobDetails",
                table: "JobPosts");
        }
    }
}
