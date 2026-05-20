using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetBuddy.Service.Transactions.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditableEntityCreatedByModifiedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "transactions",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                schema: "transactions",
                table: "Transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                schema: "transactions",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "transactions",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                schema: "transactions",
                table: "AuditLogs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "transactions",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                schema: "transactions",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                schema: "transactions",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "transactions",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                schema: "transactions",
                table: "AuditLogs");
        }
    }
}
