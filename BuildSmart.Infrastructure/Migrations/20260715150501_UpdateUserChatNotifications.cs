using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserChatNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastChatEmailSentAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "EmailOnNewChatMessage",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.Sql("UPDATE \"Users\" SET \"EmailOnNewChatMessage\" = TRUE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastChatEmailSentAt",
                table: "Users");

            migrationBuilder.AlterColumn<bool>(
                name: "EmailOnNewChatMessage",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);
        }
    }
}
