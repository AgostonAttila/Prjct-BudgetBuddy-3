using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixRlsAdminBypass : Migration
    {
        private static readonly string[] UserOwnedTables =
        [
            "Accounts",
            "Transactions",
            "Categories",
            "Budgets",
            "Investments",
            "SecurityEvents",
            "AuditLogs"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix: original admin_bypass policy used current_setting(...)::boolean which throws
            // when app.is_admin is not set (returns '' which cannot be cast to boolean).
            // Fix uses NULLIF to coerce '' → NULL before casting, then IS TRUE for safe comparison.
            // WITH CHECK added so the bypass covers INSERT/UPDATE rows, not only SELECT/DELETE.
            foreach (var table in UserOwnedTables)
            {
                migrationBuilder.Sql($@"DROP POLICY IF EXISTS {table.ToLower()}_admin_bypass ON ""{table}"";");

                migrationBuilder.Sql($@"
                    CREATE POLICY {table.ToLower()}_admin_bypass ON ""{table}""
                    USING (NULLIF(current_setting('app.is_admin', true), '')::boolean IS TRUE)
                    WITH CHECK (NULLIF(current_setting('app.is_admin', true), '')::boolean IS TRUE);
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in UserOwnedTables)
            {
                migrationBuilder.Sql($@"DROP POLICY IF EXISTS {table.ToLower()}_admin_bypass ON ""{table}"";");

                migrationBuilder.Sql($@"
                    CREATE POLICY {table.ToLower()}_admin_bypass ON ""{table}""
                    USING (current_setting('app.is_admin', true)::boolean = true);
                ");
            }
        }
    }
}
