using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class RenameProcessingJobOutboxTimestampColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartedProcessingAt",
                table: "processing_job_outbox_messages",
                newName: "started_processing_at");

            migrationBuilder.RenameColumn(
                name: "LastAttemptedAt",
                table: "processing_job_outbox_messages",
                newName: "last_attempted_at");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                table: "processing_job_outbox_messages",
                newName: "completed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "started_processing_at",
                table: "processing_job_outbox_messages",
                newName: "StartedProcessingAt");

            migrationBuilder.RenameColumn(
                name: "last_attempted_at",
                table: "processing_job_outbox_messages",
                newName: "LastAttemptedAt");

            migrationBuilder.RenameColumn(
                name: "completed_at",
                table: "processing_job_outbox_messages",
                newName: "CompletedAt");
        }
    }
}
