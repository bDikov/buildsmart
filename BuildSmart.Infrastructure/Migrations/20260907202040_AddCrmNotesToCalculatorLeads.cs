using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmNotesToCalculatorLeads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""CalculatorLeads"" ADD COLUMN IF NOT EXISTS ""AdminNotes"" character varying(4000);
                ALTER TABLE ""CalculatorLeads"" ALTER COLUMN ""AdminNotes"" TYPE character varying(4000);
                ALTER TABLE ""CalculatorLeads"" ADD COLUMN IF NOT EXISTS ""ContactedAt"" timestamp with time zone;
                ALTER TABLE ""CalculatorLeads"" ADD COLUMN IF NOT EXISTS ""IsContacted"" boolean NOT NULL DEFAULT false;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "CalculatorLeads");

            migrationBuilder.DropColumn(
                name: "ContactedAt",
                table: "CalculatorLeads");

            migrationBuilder.DropColumn(
                name: "IsContacted",
                table: "CalculatorLeads");
        }
    }
}
