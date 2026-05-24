using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTradesmanMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectMilestoneMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TradesmanId = table.Column<Guid>(type: "uuid", nullable: false),
                    TradesmanProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMilestoneMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMilestoneMedia_JobPosts_JobId",
                        column: x => x.JobId,
                        principalTable: "JobPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMilestoneMedia_TradesmanProfiles_TradesmanProfileId",
                        column: x => x.TradesmanProfileId,
                        principalTable: "TradesmanProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TradesmanMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TradesmanId = table.Column<Guid>(type: "uuid", nullable: false),
                    TradesmanProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoUrl = table.Column<string>(type: "text", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradesmanMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradesmanMedia_TradesmanProfiles_TradesmanProfileId",
                        column: x => x.TradesmanProfileId,
                        principalTable: "TradesmanProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestoneMedia_JobId",
                table: "ProjectMilestoneMedia",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestoneMedia_TradesmanProfileId",
                table: "ProjectMilestoneMedia",
                column: "TradesmanProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TradesmanMedia_TradesmanProfileId",
                table: "TradesmanMedia",
                column: "TradesmanProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectMilestoneMedia");

            migrationBuilder.DropTable(
                name: "TradesmanMedia");
        }
    }
}
