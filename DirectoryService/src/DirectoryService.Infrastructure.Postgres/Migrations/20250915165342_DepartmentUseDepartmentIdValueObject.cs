using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentUseDepartmentIdValueObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_department_locations_departments_DepartmentId1",
                table: "department_locations");

            migrationBuilder.DropForeignKey(
                name: "FK_department_positions_departments_DepartmentId1",
                table: "department_positions");

            migrationBuilder.DropForeignKey(
                name: "FK_department_positions_locations_position_id",
                table: "department_positions");

            migrationBuilder.DropForeignKey(
                name: "FK_department_positions_positions_department_id",
                table: "department_positions");

            migrationBuilder.DropIndex(
                name: "IX_department_positions_DepartmentId1",
                table: "department_positions");

            migrationBuilder.DropIndex(
                name: "IX_department_locations_DepartmentId1",
                table: "department_locations");

            migrationBuilder.DropColumn(
                name: "DepartmentId1",
                table: "department_positions");

            migrationBuilder.DropColumn(
                name: "DepartmentId1",
                table: "department_locations");

            migrationBuilder.AddForeignKey(
                name: "FK_department_positions_positions_position_id",
                table: "department_positions",
                column: "position_id",
                principalTable: "positions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_department_positions_positions_position_id",
                table: "department_positions");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId1",
                table: "department_positions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId1",
                table: "department_locations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_department_positions_DepartmentId1",
                table: "department_positions",
                column: "DepartmentId1");

            migrationBuilder.CreateIndex(
                name: "IX_department_locations_DepartmentId1",
                table: "department_locations",
                column: "DepartmentId1");

            migrationBuilder.AddForeignKey(
                name: "FK_department_locations_departments_DepartmentId1",
                table: "department_locations",
                column: "DepartmentId1",
                principalTable: "departments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_department_positions_departments_DepartmentId1",
                table: "department_positions",
                column: "DepartmentId1",
                principalTable: "departments",
                principalColumn: "id");

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
    }
}
