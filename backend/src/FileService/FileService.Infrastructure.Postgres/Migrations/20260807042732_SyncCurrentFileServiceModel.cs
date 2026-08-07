using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class SyncCurrentFileServiceModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "preview_key_bucket",
                schema: "files",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preview_key_full_path",
                schema: "files",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preview_key_key",
                schema: "files",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preview_key_prefix",
                schema: "files",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preview_key_value",
                schema: "files",
                table: "media_assets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "preview_key_bucket",
                schema: "files",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "preview_key_full_path",
                schema: "files",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "preview_key_key",
                schema: "files",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "preview_key_prefix",
                schema: "files",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "preview_key_value",
                schema: "files",
                table: "media_assets");
        }
    }
}
