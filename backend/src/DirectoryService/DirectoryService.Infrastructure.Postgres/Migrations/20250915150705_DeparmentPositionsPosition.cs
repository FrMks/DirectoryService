using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class DeparmentPositionsPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentPosition_departments_DepartmentId",
                table: "DepartmentPosition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DepartmentPosition",
                table: "DepartmentPosition");

            migrationBuilder.RenameTable(
                name: "DepartmentPosition",
                newName: "department_positions");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "department_positions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PositionId",
                table: "department_positions",
                newName: "position_id");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                table: "department_positions",
                newName: "department_id");

            migrationBuilder.RenameIndex(
                name: "IX_DepartmentPosition_DepartmentId",
                table: "department_positions",
                newName: "IX_department_positions_department_id");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId1",
                table: "department_positions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_department_positions",
                table: "department_positions",
                column: "id");

            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    update_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_positions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_department_positions_DepartmentId1",
                table: "department_positions",
                column: "DepartmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_department_positions_position_id",
                table: "department_positions",
                column: "position_id");

            migrationBuilder.AddForeignKey(
                name: "FK_department_positions_departments_DepartmentId1",
                table: "department_positions",
                column: "DepartmentId1",
                principalTable: "departments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_department_positions_departments_department_id",
                table: "department_positions",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_department_positions_locations_position_id",
                table: "department_positions",
                column: "position_id",
                principalTable: "locations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_department_positions_positions_department_id",
                table: "department_positions",
                column: "department_id",
                principalTable: "positions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_department_positions_departments_DepartmentId1",
                table: "department_positions");

            migrationBuilder.DropForeignKey(
                name: "FK_department_positions_departments_department_id",
                table: "department_positions");

            migrationBuilder.DropForeignKey(
                name: "FK_department_positions_locations_position_id",
                table: "department_positions");

            migrationBuilder.DropForeignKey(
                name: "FK_department_positions_positions_department_id",
                table: "department_positions");

            migrationBuilder.DropTable(
                name: "positions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_department_positions",
                table: "department_positions");

            migrationBuilder.DropIndex(
                name: "IX_department_positions_DepartmentId1",
                table: "department_positions");

            migrationBuilder.DropIndex(
                name: "IX_department_positions_position_id",
                table: "department_positions");

            migrationBuilder.DropColumn(
                name: "DepartmentId1",
                table: "department_positions");

            migrationBuilder.RenameTable(
                name: "department_positions",
                newName: "DepartmentPosition");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "DepartmentPosition",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "position_id",
                table: "DepartmentPosition",
                newName: "PositionId");

            migrationBuilder.RenameColumn(
                name: "department_id",
                table: "DepartmentPosition",
                newName: "DepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_department_positions_department_id",
                table: "DepartmentPosition",
                newName: "IX_DepartmentPosition_DepartmentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DepartmentPosition",
                table: "DepartmentPosition",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentPosition_departments_DepartmentId",
                table: "DepartmentPosition",
                column: "DepartmentId",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
