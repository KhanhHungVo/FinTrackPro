using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrackPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionFieldsToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentCustomerId",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentSubscriptionId",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Plan",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_PaymentCustomerId",
                table: "Users",
                column: "PaymentCustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppUsers_PaymentCustomerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PaymentCustomerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PaymentSubscriptionId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Plan",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionExpiresAt",
                table: "Users");
        }
    }
}
