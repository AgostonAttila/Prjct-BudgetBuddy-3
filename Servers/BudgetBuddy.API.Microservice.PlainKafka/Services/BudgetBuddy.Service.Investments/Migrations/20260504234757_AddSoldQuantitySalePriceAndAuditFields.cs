using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace BudgetBuddy.Service.Investments.Migrations
{
    /// <inheritdoc />
    public partial class AddSoldQuantitySalePriceAndAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "investments",
                table: "Investments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                schema: "investments",
                table: "Investments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                schema: "investments",
                table: "Investments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoldQuantity",
                schema: "investments",
                table: "Investments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "investments",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                schema: "investments",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "investments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt",
                schema: "investments",
                table: "OutboxMessages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Processor_Optimized",
                schema: "investments",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "RetryCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "investments");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "investments",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                schema: "investments",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                schema: "investments",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "SoldQuantity",
                schema: "investments",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "investments",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                schema: "investments",
                table: "AuditLogs");
        }
    }
}
