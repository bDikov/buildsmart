using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
	/// <inheritdoc />
	public partial class InitialCreate : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "ServiceCategories",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
					Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
					CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_ServiceCategories", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Users",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
					LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
					Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
					HashedPassword = table.Column<string>(type: "text", nullable: false),
					PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
					Role = table.Column<int>(type: "integer", nullable: false),
					Bio = table.Column<string>(type: "text", nullable: true),
					Location = table.Column<string>(type: "text", nullable: true),
					ProfilePictureUrl = table.Column<string>(type: "text", nullable: true),
					CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Users", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "TradesmanProfiles",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					UserId = table.Column<Guid>(type: "uuid", nullable: false),
					ServiceCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
					AverageRating = table.Column<double>(type: "double precision", precision: 3, scale: 2, nullable: false),
					IsVerified = table.Column<bool>(type: "boolean", nullable: false),
					CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_TradesmanProfiles", x => x.Id);
					table.ForeignKey(
						name: "FK_TradesmanProfiles_ServiceCategories_ServiceCategoryId",
						column: x => x.ServiceCategoryId,
						principalTable: "ServiceCategories",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_TradesmanProfiles_Users_UserId",
						column: x => x.UserId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Bookings",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					RequestedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					ScheduledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
					JobDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
					Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
					Amount_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
					Amount_Subtotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
					Amount_Tax = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
					Amount_Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
					HomeownerId = table.Column<Guid>(type: "uuid", nullable: false),
					TradesmanProfileId = table.Column<Guid>(type: "uuid", nullable: false),
					CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Bookings", x => x.Id);
					table.ForeignKey(
						name: "FK_Bookings_TradesmanProfiles_TradesmanProfileId",
						column: x => x.TradesmanProfileId,
						principalTable: "TradesmanProfiles",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
					table.ForeignKey(
						name: "FK_Bookings_Users_HomeownerId",
						column: x => x.HomeownerId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.CreateTable(
				name: "PortfolioEntries",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
					Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
					ImageUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
					VideoUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
					TradesmanProfileId = table.Column<Guid>(type: "uuid", nullable: false),
					CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_PortfolioEntries", x => x.Id);
					table.ForeignKey(
						name: "FK_PortfolioEntries_TradesmanProfiles_TradesmanProfileId",
						column: x => x.TradesmanProfileId,
						principalTable: "TradesmanProfiles",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "Reviews",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					Rating = table.Column<int>(type: "integer", nullable: false),
					Comment = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
					BookingId = table.Column<Guid>(type: "uuid", nullable: false),
					HomeownerId = table.Column<Guid>(type: "uuid", nullable: false),
					TradesmanProfileId = table.Column<Guid>(type: "uuid", nullable: false),
					CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Reviews", x => x.Id);
					table.ForeignKey(
						name: "FK_Reviews_Bookings_BookingId",
						column: x => x.BookingId,
						principalTable: "Bookings",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_Reviews_TradesmanProfiles_TradesmanProfileId",
						column: x => x.TradesmanProfileId,
						principalTable: "TradesmanProfiles",
						principalColumn: "Id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "FK_Reviews_Users_HomeownerId",
						column: x => x.HomeownerId,
						principalTable: "Users",
						principalColumn: "Id",
						onDelete: ReferentialAction.Restrict);
				});

			migrationBuilder.CreateIndex(
				name: "IX_Bookings_HomeownerId",
				table: "Bookings",
				column: "HomeownerId");

			migrationBuilder.CreateIndex(
				name: "IX_Bookings_TradesmanProfileId",
				table: "Bookings",
				column: "TradesmanProfileId");

			migrationBuilder.CreateIndex(
				name: "IX_PortfolioEntries_TradesmanProfileId",
				table: "PortfolioEntries",
				column: "TradesmanProfileId");

			migrationBuilder.CreateIndex(
				name: "IX_Reviews_BookingId",
				table: "Reviews",
				column: "BookingId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_Reviews_HomeownerId",
				table: "Reviews",
				column: "HomeownerId");

			migrationBuilder.CreateIndex(
				name: "IX_Reviews_TradesmanProfileId",
				table: "Reviews",
				column: "TradesmanProfileId");

			migrationBuilder.CreateIndex(
				name: "IX_ServiceCategories_Name",
				table: "ServiceCategories",
				column: "Name",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_TradesmanProfiles_ServiceCategoryId",
				table: "TradesmanProfiles",
				column: "ServiceCategoryId");

			migrationBuilder.CreateIndex(
				name: "IX_TradesmanProfiles_UserId",
				table: "TradesmanProfiles",
				column: "UserId",
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_Users_Email",
				table: "Users",
				column: "Email",
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "PortfolioEntries");

			migrationBuilder.DropTable(
				name: "Reviews");

			migrationBuilder.DropTable(
				name: "Bookings");

			migrationBuilder.DropTable(
				name: "TradesmanProfiles");

			migrationBuilder.DropTable(
				name: "ServiceCategories");

			migrationBuilder.DropTable(
				name: "Users");
		}
	}
}