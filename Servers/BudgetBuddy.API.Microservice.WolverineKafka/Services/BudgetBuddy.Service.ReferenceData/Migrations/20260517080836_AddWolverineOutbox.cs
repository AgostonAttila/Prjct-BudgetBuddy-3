using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetBuddy.Service.ReferenceData.Migrations
{
    /// <inheritdoc />
    public partial class AddWolverineOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: referencedata schema never had a custom OutboxMessages table.
            // Wolverine envelope storage is created at runtime, not via EF migrations.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: nothing was created in Up.
        }
    }
}
