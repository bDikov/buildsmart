using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildSmart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLandingPagesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CategoryStatus",
                table: "JobPosts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "LandingPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TitleBg = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SubtitleBg = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SubtitleEn = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BadgeBg = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BadgeEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HeroImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    HeroVideoUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MediaGalleryJson = table.Column<string>(type: "text", nullable: false),
                    FeaturesJson = table.Column<string>(type: "text", nullable: false),
                    CtaTextBg = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CtaTextEn = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CtaLink = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MetaTitleBg = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MetaTitleEn = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MetaDescriptionBg = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MetaDescriptionEn = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandingPages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LandingPages_IsPublished",
                table: "LandingPages",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_LandingPages_PageType",
                table: "LandingPages",
                column: "PageType");

            migrationBuilder.CreateIndex(
                name: "IX_LandingPages_Slug",
                table: "LandingPages",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LandingPages");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryStatus",
                table: "JobPosts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);
        }
    }
}
