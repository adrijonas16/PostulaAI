using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostulaAI.Api.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationNotes",
                table: "job_postings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            // Existing postings were saved but never marked as applied.
            migrationBuilder.AddColumn<string>(
                name: "ApplicationStatus",
                table: "job_postings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Saved");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApplicationUpdatedAt",
                table: "job_postings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "AppliedOn",
                table: "job_postings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedCoverLetter",
                table: "job_postings",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedSummary",
                table: "job_postings",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_postings_UserId_ApplicationStatus",
                table: "job_postings",
                columns: new[] { "UserId", "ApplicationStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_job_postings_UserId_ApplicationStatus",
                table: "job_postings");

            migrationBuilder.DropColumn(name: "ApplicationNotes", table: "job_postings");
            migrationBuilder.DropColumn(name: "ApplicationStatus", table: "job_postings");
            migrationBuilder.DropColumn(name: "ApplicationUpdatedAt", table: "job_postings");
            migrationBuilder.DropColumn(name: "AppliedOn", table: "job_postings");
            migrationBuilder.DropColumn(name: "SubmittedCoverLetter", table: "job_postings");
            migrationBuilder.DropColumn(name: "SubmittedSummary", table: "job_postings");
        }
    }
}
