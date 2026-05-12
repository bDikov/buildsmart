using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookingCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Bids_BidId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_JobPosts_JobPostId",
                table: "Bookings");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Bids_BidId",
                table: "Bookings",
                column: "BidId",
                principalTable: "Bids",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_JobPosts_JobPostId",
                table: "Bookings",
                column: "JobPostId",
                principalTable: "JobPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Bids_BidId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_JobPosts_JobPostId",
                table: "Bookings");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Bids_BidId",
                table: "Bookings",
                column: "BidId",
                principalTable: "Bids",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_JobPosts_JobPostId",
                table: "Bookings",
                column: "JobPostId",
                principalTable: "JobPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
