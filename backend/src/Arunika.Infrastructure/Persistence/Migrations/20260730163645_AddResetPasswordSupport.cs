using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arunika.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResetPasswordSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResetTokenExpiresAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill EmailVerified = true for users who existed before the OTP
            // column was added (EmailVerified = false and never issued an OTP).
            // This lets pre-OTP users sign in without having to verify.
            migrationBuilder.Sql("""
                UPDATE users
                SET "EmailVerified" = true
                WHERE "EmailVerified" = false AND "OtpCode" IS NULL
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: we do NOT revert the EmailVerified backfill in Down.
            // There is no reliable way to know which rows were backfilled vs
            // legitimately verified, and reverting would break existing users.
            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiresAt",
                table: "users");
        }
    }
}
