using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetBuddy.Service.Transactions.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionSagaStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SagaStep",
                schema: "transactions",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SagaStep_Pending",
                schema: "transactions",
                table: "Transactions",
                columns: new[] { "SagaStep", "CreatedAt" },
                filter: "\"SagaStep\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_SagaStep_Pending",
                schema: "transactions",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SagaStep",
                schema: "transactions",
                table: "Transactions");
        }
    }
}
