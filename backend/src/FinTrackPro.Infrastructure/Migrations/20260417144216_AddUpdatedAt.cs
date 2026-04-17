using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrackPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add DEFAULT NOW() safety net to all existing CreatedAt columns
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "WatchedSymbols",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TransactionCategories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Trades",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Signals",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "NotificationPreferences",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Budgets",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            // Add CreatedAt to UserIdentities (new column — no existing data, no backfill needed)
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UserIdentities",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            // Add UpdatedAt to mutable tables:
            // Step 1 — add as nullable (no default — avoids NOW() backfill on existing rows)
            // Step 2 — backfill from CreatedAt so existing rows reflect "last updated = when created"
            // Step 3 — make non-nullable with DEFAULT NOW() as safety net for future inserts

            foreach (var table in new[] { "Users", "Transactions", "TransactionCategories", "Trades", "NotificationPreferences", "Budgets" })
            {
                migrationBuilder.AddColumn<DateTime>(
                    name: "UpdatedAt",
                    table: table,
                    type: "timestamp with time zone",
                    nullable: true);

                migrationBuilder.Sql($@"UPDATE ""{table}"" SET ""UpdatedAt"" = ""CreatedAt"";");

                migrationBuilder.AlterColumn<DateTime>(
                    name: "UpdatedAt",
                    table: table,
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "NOW()",
                    oldClrType: typeof(DateTime),
                    oldType: "timestamp with time zone",
                    oldNullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UserIdentities");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TransactionCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Budgets");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "WatchedSymbols",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TransactionCategories",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Trades",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Signals",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "NotificationPreferences",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Budgets",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");
        }
    }
}
