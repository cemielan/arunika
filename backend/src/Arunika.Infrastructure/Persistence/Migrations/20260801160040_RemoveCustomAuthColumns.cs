using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arunika.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomAuthColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtpCode",
                table: "users");

            migrationBuilder.DropColumn(
                name: "OtpExpiresAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiresAt",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OtpCode",
                table: "users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OtpExpiresAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

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
        }
    }
}
