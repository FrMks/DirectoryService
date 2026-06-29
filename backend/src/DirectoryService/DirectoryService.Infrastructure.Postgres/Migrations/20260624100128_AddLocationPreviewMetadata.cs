using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationPreviewMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "preview_attached_at",
                table: "locations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preview_content_type",
                table: "locations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preview_file_name",
                table: "locations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "preview_last_verified_at",
                table: "locations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "preview_size",
                table: "locations",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "preview_attached_at",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "preview_content_type",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "preview_file_name",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "preview_last_verified_at",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "preview_size",
                table: "locations");
        }
    }
}
