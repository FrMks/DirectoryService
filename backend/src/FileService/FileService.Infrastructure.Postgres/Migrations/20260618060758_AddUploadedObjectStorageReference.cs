using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadedObjectStorageReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "uploaded_object_content_type",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "uploaded_object_etag",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "uploaded_object_key_bucket",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "uploaded_object_key_full_path",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "uploaded_object_key_key",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "uploaded_object_key_prefix",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "uploaded_object_key_value",
                table: "media_assets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "uploaded_object_size_bytes",
                table: "media_assets",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "uploaded_object_content_type",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "uploaded_object_etag",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "uploaded_object_key_bucket",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "uploaded_object_key_full_path",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "uploaded_object_key_key",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "uploaded_object_key_prefix",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "uploaded_object_key_value",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "uploaded_object_size_bytes",
                table: "media_assets");
        }
    }
}
