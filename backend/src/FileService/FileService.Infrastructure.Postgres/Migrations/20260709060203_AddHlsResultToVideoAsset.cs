using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddHlsResultToVideoAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "hls_result_manifest_key_bucket",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hls_result_manifest_key_full_path",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hls_result_manifest_key_key",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hls_result_manifest_key_prefix",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hls_result_manifest_key_value",
                table: "media_assets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hls_result_manifest_key_bucket",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "hls_result_manifest_key_full_path",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "hls_result_manifest_key_key",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "hls_result_manifest_key_prefix",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "hls_result_manifest_key_value",
                table: "media_assets");
        }
    }
}
