using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhereIsMyMoney.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeLastImportAtUtcNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rows that were never imported carry DateTime.MinValue as a sentinel —
            // reset them to NULL so the import logic can distinguish them cleanly.
            migrationBuilder.Sql(
                """
                UPDATE "EnableBankingSessions"
                SET "LastImportAtUtc" = NULL
                WHERE "LastImportAtUtc" = '-infinity'
                   OR "LastImportAtUtc" = '0001-01-01 00:00:00+00';
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastImportAtUtc",
                table: "EnableBankingSessions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastImportAtUtc",
                table: "EnableBankingSessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldNullable: true,
                oldType: "timestamp with time zone");
        }
    }
}
