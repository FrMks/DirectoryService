using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueVideoAssetIdIndexAndAddVideoProcessingIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_video_processing_video_asset_id",
                schema: "files",
                table: "video_processing");

            migrationBuilder.RenameIndex(
                name: "IX_processing_steps_video_processing_id",
                schema: "files",
                table: "processing_steps",
                newName: "ix_processing_steps_video_processing_id");

            migrationBuilder.CreateIndex(
                name: "ux_video_processing_video_asset_id",
                schema: "files",
                table: "video_processing",
                column: "video_asset_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_video_processing_video_asset_id",
                schema: "files",
                table: "video_processing");

            migrationBuilder.RenameIndex(
                name: "ix_processing_steps_video_processing_id",
                schema: "files",
                table: "processing_steps",
                newName: "IX_processing_steps_video_processing_id");

            migrationBuilder.CreateIndex(
                name: "ix_video_processing_video_asset_id",
                schema: "files",
                table: "video_processing",
                column: "video_asset_id");
        }
    }
}
