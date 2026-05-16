using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetBuddy.Service.Transactions.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionIsHidden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Column was added directly via a prior migration; this migration serves as a marker only.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No Down migration needed — this column addition is forward-only.
        }
    }
}
