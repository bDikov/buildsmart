using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class some : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TradesmanProfiles_ServiceCategories_ServiceCategoryId",
                table: "TradesmanProfiles");

            migrationBuilder.DropIndex(
                name: "IX_TradesmanProfiles_ServiceCategoryId",
                table: "TradesmanProfiles");

            migrationBuilder.DropColumn(
                name: "ServiceCategoryId",
                table: "TradesmanProfiles");

            migrationBuilder.RenameColumn(
                name: "Amount_Total",
                table: "Bookings",
                newName: "TotalEscrowAmount_Total");

            migrationBuilder.RenameColumn(
                name: "Amount_Tax",
                table: "Bookings",
                newName: "TotalEscrowAmount_Tax");

            migrationBuilder.RenameColumn(
                name: "Amount_Subtotal",
                table: "Bookings",
                newName: "TotalEscrowAmount_Subtotal");

            migrationBuilder.RenameColumn(
                name: "Amount_Currency",
                table: "Bookings",
                newName: "TotalEscrowAmount_Currency");

            migrationBuilder.AddColumn<string>(
                name: "TemplateStructure",
                table: "ServiceCategories",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalEscrowAmount_Total",
                table: "Bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalEscrowAmount_Tax",
                table: "Bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalEscrowAmount_Subtotal",
                table: "Bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "AgreedBidAmount_Currency",
                table: "Bookings",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "AgreedBidAmount_Subtotal",
                table: "Bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AgreedBidAmount_Tax",
                table: "Bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AgreedBidAmount_Total",
                table: "Bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsFunded",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlatformFeeHomeowner_Currency",
                table: "Bookings",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeeHomeowner_Subtotal",
                table: "Bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeeHomeowner_Tax",
                table: "Bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeeHomeowner_Total",
                table: "Bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PlatformFeeTradesman_Currency",
                table: "Bookings",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeeTradesman_Subtotal",
                table: "Bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeeTradesman_Tax",
                table: "Bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeeTradesman_Total",
                table: "Bookings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ChangeOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewTotalAmount_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    NewTotalAmount_Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NewTotalAmount_Tax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NewTotalAmount_Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DifferenceAmount_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DifferenceAmount_Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DifferenceAmount_Tax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DifferenceAmount_Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeOrders_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HomeownerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeownerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeownerProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    HomeownerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Users_HomeownerId",
                        column: x => x.HomeownerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TradesmanSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TradesmanProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationStatus = table.Column<int>(type: "integer", nullable: false),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradesmanSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradesmanSkills_ServiceCategories_ServiceCategoryId",
                        column: x => x.ServiceCategoryId,
                        principalTable: "ServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradesmanSkills_TradesmanProfiles_TradesmanProfileId",
                        column: x => x.TradesmanProfileId,
                        principalTable: "TradesmanProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HomeownerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JobDetails = table.Column<string>(type: "jsonb", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ImageUrls = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EstimatedBudget_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    EstimatedBudget_Subtotal = table.Column<decimal>(type: "numeric", nullable: true),
                    EstimatedBudget_Tax = table.Column<decimal>(type: "numeric", nullable: true),
                    EstimatedBudget_Total = table.Column<decimal>(type: "numeric", nullable: true),
                    AmendmentCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobPosts_HomeownerProfiles_HomeownerProfileId",
                        column: x => x.HomeownerProfileId,
                        principalTable: "HomeownerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobPosts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobPosts_ServiceCategories_ServiceCategoryId",
                        column: x => x.ServiceCategoryId,
                        principalTable: "ServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    TradesmanProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Amount_Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount_Tax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount_Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LinkedAmendmentVersion = table.Column<int>(type: "integer", nullable: false),
                    IsAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRejected = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bids_JobPosts_JobPostId",
                        column: x => x.JobPostId,
                        principalTable: "JobPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bids_TradesmanProfiles_TradesmanProfileId",
                        column: x => x.TradesmanProfileId,
                        principalTable: "TradesmanProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobPostQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    TradesmanProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionText = table.Column<string>(type: "text", nullable: false),
                    AnswerText = table.Column<string>(type: "text", nullable: true),
                    AnsweredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPostQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobPostQuestions_JobPosts_JobPostId",
                        column: x => x.JobPostId,
                        principalTable: "JobPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobPostQuestions_TradesmanProfiles_TradesmanProfileId",
                        column: x => x.TradesmanProfileId,
                        principalTable: "TradesmanProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bids_JobPostId",
                table: "Bids",
                column: "JobPostId");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_TradesmanProfileId",
                table: "Bids",
                column: "TradesmanProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeOrders_BookingId",
                table: "ChangeOrders",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeownerProfiles_UserId",
                table: "HomeownerProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobPostQuestions_JobPostId",
                table: "JobPostQuestions",
                column: "JobPostId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostQuestions_TradesmanProfileId",
                table: "JobPostQuestions",
                column: "TradesmanProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_HomeownerProfileId",
                table: "JobPosts",
                column: "HomeownerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_ProjectId",
                table: "JobPosts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPosts_ServiceCategoryId",
                table: "JobPosts",
                column: "ServiceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_HomeownerId",
                table: "Projects",
                column: "HomeownerId");

            migrationBuilder.CreateIndex(
                name: "IX_TradesmanSkills_ServiceCategoryId",
                table: "TradesmanSkills",
                column: "ServiceCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TradesmanSkills_TradesmanProfileId",
                table: "TradesmanSkills",
                column: "TradesmanProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bids");

            migrationBuilder.DropTable(
                name: "ChangeOrders");

            migrationBuilder.DropTable(
                name: "JobPostQuestions");

            migrationBuilder.DropTable(
                name: "TradesmanSkills");

            migrationBuilder.DropTable(
                name: "JobPosts");

            migrationBuilder.DropTable(
                name: "HomeownerProfiles");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropColumn(
                name: "TemplateStructure",
                table: "ServiceCategories");

            migrationBuilder.DropColumn(
                name: "AgreedBidAmount_Currency",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AgreedBidAmount_Subtotal",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AgreedBidAmount_Tax",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AgreedBidAmount_Total",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsFunded",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PlatformFeeHomeowner_Currency",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PlatformFeeHomeowner_Subtotal",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PlatformFeeHomeowner_Tax",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PlatformFeeHomeowner_Total",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PlatformFeeTradesman_Currency",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PlatformFeeTradesman_Subtotal",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PlatformFeeTradesman_Tax",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PlatformFeeTradesman_Total",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "TotalEscrowAmount_Total",
                table: "Bookings",
                newName: "Amount_Total");

            migrationBuilder.RenameColumn(
                name: "TotalEscrowAmount_Tax",
                table: "Bookings",
                newName: "Amount_Tax");

            migrationBuilder.RenameColumn(
                name: "TotalEscrowAmount_Subtotal",
                table: "Bookings",
                newName: "Amount_Subtotal");

            migrationBuilder.RenameColumn(
                name: "TotalEscrowAmount_Currency",
                table: "Bookings",
                newName: "Amount_Currency");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceCategoryId",
                table: "TradesmanProfiles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount_Total",
                table: "Bookings",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount_Tax",
                table: "Bookings",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount_Subtotal",
                table: "Bookings",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_TradesmanProfiles_ServiceCategoryId",
                table: "TradesmanProfiles",
                column: "ServiceCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_TradesmanProfiles_ServiceCategories_ServiceCategoryId",
                table: "TradesmanProfiles",
                column: "ServiceCategoryId",
                principalTable: "ServiceCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
