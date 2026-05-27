using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_type = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    raw_key_bucket = table.Column<string>(type: "text", nullable: false),
                    raw_key_prefix = table.Column<string>(type: "text", nullable: false),
                    raw_key_key = table.Column<string>(type: "text", nullable: false),
                    raw_key_value = table.Column<string>(type: "text", nullable: false),
                    raw_key_full_path = table.Column<string>(type: "text", nullable: false),
                    final_key_bucket = table.Column<string>(type: "text", nullable: false),
                    final_key_prefix = table.Column<string>(type: "text", nullable: false),
                    final_key_key = table.Column<string>(type: "text", nullable: false),
                    final_key_value = table.Column<string>(type: "text", nullable: false),
                    final_key_full_path = table.Column<string>(type: "text", nullable: false),
                    owner_context = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    hls_root_key_bucket = table.Column<string>(type: "text", nullable: true),
                    hls_root_key_prefix = table.Column<string>(type: "text", nullable: true),
                    hls_root_key_key = table.Column<string>(type: "text", nullable: true),
                    hls_root_key_value = table.Column<string>(type: "text", nullable: true),
                    hls_root_key_full_path = table.Column<string>(type: "text", nullable: true),
                    media_data = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_assets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_status_created_at",
                table: "media_assets",
                columns: new[] { "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_assets");
        }
    }
}
