using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetBuddy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRowLevelSecurityPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable RLS and create policies for all user-owned tables
            // Note: Only tables with UserId column (user-scoped data)
            var userOwnedTables = new[]
            {
                "Accounts",         // User accounts
                "Transactions",     // User transactions
                "Categories",       // User categories
                "Budgets",          // User budgets
                "Investments",      // User investments
                "SecurityEvents",   // User security events (UserId nullable for anonymous events)
                "AuditLogs"         // Entity audit logs (UserId nullable for system operations)
            };

            // Excluded:
            // - CategoryTypes: No UserId, belongs to Category (child entity)
            // - Currencies: Global entity, shared across all users
            // - AspNetUsers/Roles/etc: Identity framework tables, no RLS needed

            foreach (var table in userOwnedTables)
            {
                // Enable Row Level Security
                migrationBuilder.Sql($@"ALTER TABLE ""{table}"" ENABLE ROW LEVEL SECURITY;");

                // Create policy: Users can only access their own data
                // Note: UserId is TEXT type, not UUID, so no casting needed
                migrationBuilder.Sql($@"
                    CREATE POLICY {table.ToLower()}_user_isolation ON ""{table}""
                    USING (""UserId"" IS NOT NULL AND ""UserId"" = current_setting('app.current_user_id', true));
                ");

                // Create policy: Admins can access all data
                migrationBuilder.Sql($@"
                    CREATE POLICY {table.ToLower()}_admin_bypass ON ""{table}""
                    USING (current_setting('app.is_admin', true)::boolean = true);
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Drop policies and disable RLS
            var userOwnedTables = new[]
            {
                "Accounts",
                "Transactions",
                "Categories",
                "Budgets",
                "Investments",
                "SecurityEvents",
                "AuditLogs"
            };

            foreach (var table in userOwnedTables)
            {
                // Drop policies
                migrationBuilder.Sql($@"DROP POLICY IF EXISTS {table.ToLower()}_user_isolation ON ""{table}"";");
                migrationBuilder.Sql($@"DROP POLICY IF EXISTS {table.ToLower()}_admin_bypass ON ""{table}"";");

                // Disable Row Level Security
                migrationBuilder.Sql($@"ALTER TABLE ""{table}"" DISABLE ROW LEVEL SECURITY;");
            }
        }
    }
}
