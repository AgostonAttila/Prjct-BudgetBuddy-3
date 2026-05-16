using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetBuddy.Service.Accounts.Migrations
{
    /// <inheritdoc />
    public partial class AddLastProcessedMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LastProcessedMessageId",
                schema: "accounts",
                table: "account_transaction_totals",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastProcessedMessageId",
                schema: "accounts",
                table: "account_transaction_totals");
        }
    }
}
