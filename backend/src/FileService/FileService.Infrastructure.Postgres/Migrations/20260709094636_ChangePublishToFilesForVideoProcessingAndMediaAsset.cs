using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class ChangePublishToFilesForVideoProcessingAndMediaAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "files");

            migrationBuilder.RenameTable(
                name: "video_processing",
                newName: "video_processing",
                newSchema: "files");

            migrationBuilder.RenameTable(
                name: "processing_steps",
                newName: "processing_steps",
                newSchema: "files");

            migrationBuilder.RenameTable(
                name: "media_assets",
                newName: "media_assets",
                newSchema: "files");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "video_processing",
                schema: "files",
                newName: "video_processing");

            migrationBuilder.RenameTable(
                name: "processing_steps",
                schema: "files",
                newName: "processing_steps");

            migrationBuilder.RenameTable(
                name: "media_assets",
                schema: "files",
                newName: "media_assets");
        }
    }
}
