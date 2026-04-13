using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetBuddy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncEncryptionConverterSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Syncs EF model snapshot after IEncryptionService was made non-nullable.
            // Also ensures SoldDate exists — AddInvestmentSoldDate was manually created
            // without a designer file so it may not have been applied to the DB.
            migrationBuilder.Sql(@"ALTER TABLE ""Investments"" ADD COLUMN IF NOT EXISTS ""SoldDate"" date;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SoldDate", table: "Investments");
        }
    }
}
