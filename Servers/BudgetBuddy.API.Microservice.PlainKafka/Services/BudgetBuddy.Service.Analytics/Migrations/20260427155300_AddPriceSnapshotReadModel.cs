using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace BudgetBuddy.Service.Analytics.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceSnapshotReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "price_snapshots",
                schema: "analytics",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Date = table.Column<LocalDate>(type: "date", nullable: false),
                    PriceUsd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    SyncedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_snapshots", x => new { x.Symbol, x.Date });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsPriceSnapshots_Symbol",
                schema: "analytics",
                table: "price_snapshots",
                column: "Symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "price_snapshots",
                schema: "analytics");
        }
    }
}
