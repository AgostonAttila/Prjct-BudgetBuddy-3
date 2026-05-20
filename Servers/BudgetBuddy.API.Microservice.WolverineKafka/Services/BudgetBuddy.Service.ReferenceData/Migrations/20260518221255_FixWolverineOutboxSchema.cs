using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetBuddy.Service.ReferenceData.Migrations
{
    /// <inheritdoc />
    public partial class FixWolverineOutboxSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No schema changes needed — Wolverine manages its own tables in the "wolverine" schema.
            // This migration only updates the EF Core model snapshot so that MapWolverineEnvelopeStorage
            // points to "wolverine" instead of the service schema, aligning with PersistMessagesWithPostgresql.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: reverting the model snapshot change requires no SQL.
        }
    }
}
