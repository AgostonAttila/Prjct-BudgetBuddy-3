using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetBuddy.Service.ReferenceData.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditableEntityCreatedByModifiedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "referencedata",
                table: "Currencies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                schema: "referencedata",
                table: "Currencies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "referencedata",
                table: "CategoryTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                schema: "referencedata",
                table: "CategoryTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "referencedata",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                schema: "referencedata",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "referencedata",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                schema: "referencedata",
                table: "AuditLogs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "referencedata",
                table: "Currencies");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                schema: "referencedata",
                table: "Currencies");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "referencedata",
                table: "CategoryTypes");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                schema: "referencedata",
                table: "CategoryTypes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "referencedata",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                schema: "referencedata",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "referencedata",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                schema: "referencedata",
                table: "AuditLogs");
        }
    }
}
