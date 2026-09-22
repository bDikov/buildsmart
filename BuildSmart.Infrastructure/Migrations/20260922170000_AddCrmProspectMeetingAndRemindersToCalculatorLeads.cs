using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmProspectMeetingAndRemindersToCalculatorLeads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""CalculatorLeads"" ADD COLUMN IF NOT EXISTS ""CallOutcome"" character varying(50);
                ALTER TABLE ""CalculatorLeads"" ADD COLUMN IF NOT EXISTS ""ProspectRating"" integer;
                ALTER TABLE ""CalculatorLeads"" ADD COLUMN IF NOT EXISTS ""MeetingStatus"" character varying(50);
                ALTER TABLE ""CalculatorLeads"" ADD COLUMN IF NOT EXISTS ""MeetingDate"" timestamp with time zone;
                ALTER TABLE ""CalculatorLeads"" ADD COLUMN IF NOT EXISTS ""MeetingNotes"" character varying(2000);
                ALTER TABLE ""CalculatorLeads"" ADD COLUMN IF NOT EXISTS ""ReadyToStartTimeline"" character varying(100);
                ALTER TABLE ""CalculatorLeads"" ADD COLUMN IF NOT EXISTS ""FollowUpDate"" timestamp with time zone;
                ALTER TABLE ""CalculatorLeads"" ADD COLUMN IF NOT EXISTS ""FollowUpReminderSent"" boolean NOT NULL DEFAULT false;

                CREATE INDEX IF NOT EXISTS ""IX_CalculatorLeads_ProspectRating"" ON ""CalculatorLeads"" (""ProspectRating"");
                CREATE INDEX IF NOT EXISTS ""IX_CalculatorLeads_MeetingStatus"" ON ""CalculatorLeads"" (""MeetingStatus"");
                CREATE INDEX IF NOT EXISTS ""IX_CalculatorLeads_FollowUpDate"" ON ""CalculatorLeads"" (""FollowUpDate"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CalculatorLeads_ProspectRating",
                table: "CalculatorLeads");

            migrationBuilder.DropIndex(
                name: "IX_CalculatorLeads_MeetingStatus",
                table: "CalculatorLeads");

            migrationBuilder.DropIndex(
                name: "IX_CalculatorLeads_FollowUpDate",
                table: "CalculatorLeads");

            migrationBuilder.DropColumn(
                name: "CallOutcome",
                table: "CalculatorLeads");

            migrationBuilder.DropColumn(
                name: "ProspectRating",
                table: "CalculatorLeads");

            migrationBuilder.DropColumn(
                name: "MeetingStatus",
                table: "CalculatorLeads");

            migrationBuilder.DropColumn(
                name: "MeetingDate",
                table: "CalculatorLeads");

            migrationBuilder.DropColumn(
                name: "MeetingNotes",
                table: "CalculatorLeads");

            migrationBuilder.DropColumn(
                name: "ReadyToStartTimeline",
                table: "CalculatorLeads");

            migrationBuilder.DropColumn(
                name: "FollowUpDate",
                table: "CalculatorLeads");

            migrationBuilder.DropColumn(
                name: "FollowUpReminderSent",
                table: "CalculatorLeads");
        }
    }
}
