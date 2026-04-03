using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnsureSoldDateColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AddInvestmentSoldDate was manually created without a designer file and its
            // SyncEncryptionConverterSnapshot fallback ran as empty — column may be missing.
            migrationBuilder.Sql(@"ALTER TABLE ""Investments"" ADD COLUMN IF NOT EXISTS ""SoldDate"" date;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SoldDate", table: "Investments");
        }
    }
}
