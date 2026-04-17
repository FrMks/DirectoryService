using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentLocationConfigurationWithIDLocationIdDepartmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentLocation_locations_LocationId",
                table: "DepartmentLocation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DepartmentLocation",
                table: "DepartmentLocation");

            migrationBuilder.RenameTable(
                name: "DepartmentLocation",
                newName: "department_locations");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "department_locations",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "LocationId",
                table: "department_locations",
                newName: "location_id");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                table: "department_locations",
                newName: "department_id");

            migrationBuilder.RenameIndex(
                name: "IX_DepartmentLocation_LocationId",
                table: "department_locations",
                newName: "IX_department_locations_location_id");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId1",
                table: "department_locations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_department_locations",
                table: "department_locations",
                column: "id");

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    identifier = table.Column<string>(type: "text", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    path = table.Column<string>(type: "text", nullable: false),
                    depth = table.Column<short>(type: "smallint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_department", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentPosition",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentPosition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentPosition_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_department_locations_department_id",
                table: "department_locations",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_department_locations_DepartmentId1",
                table: "department_locations",
                column: "DepartmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentPosition_DepartmentId",
                table: "DepartmentPosition",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_department_locations_departments_DepartmentId1",
                table: "department_locations",
                column: "DepartmentId1",
                principalTable: "departments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_department_locations_departments_department_id",
                table: "department_locations",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_department_locations_locations_location_id",
                table: "department_locations",
                column: "location_id",
                principalTable: "locations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_department_locations_departments_DepartmentId1",
                table: "department_locations");

            migrationBuilder.DropForeignKey(
                name: "FK_department_locations_departments_department_id",
                table: "department_locations");

            migrationBuilder.DropForeignKey(
                name: "FK_department_locations_locations_location_id",
                table: "department_locations");

            migrationBuilder.DropTable(
                name: "DepartmentPosition");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropPrimaryKey(
                name: "pk_department_locations",
                table: "department_locations");

            migrationBuilder.DropIndex(
                name: "IX_department_locations_department_id",
                table: "department_locations");

            migrationBuilder.DropIndex(
                name: "IX_department_locations_DepartmentId1",
                table: "department_locations");

            migrationBuilder.DropColumn(
                name: "DepartmentId1",
                table: "department_locations");

            migrationBuilder.RenameTable(
                name: "department_locations",
                newName: "DepartmentLocation");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "DepartmentLocation",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "location_id",
                table: "DepartmentLocation",
                newName: "LocationId");

            migrationBuilder.RenameColumn(
                name: "department_id",
                table: "DepartmentLocation",
                newName: "DepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_department_locations_location_id",
                table: "DepartmentLocation",
                newName: "IX_DepartmentLocation_LocationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DepartmentLocation",
                table: "DepartmentLocation",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentLocation_locations_LocationId",
                table: "DepartmentLocation",
                column: "LocationId",
                principalTable: "locations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
