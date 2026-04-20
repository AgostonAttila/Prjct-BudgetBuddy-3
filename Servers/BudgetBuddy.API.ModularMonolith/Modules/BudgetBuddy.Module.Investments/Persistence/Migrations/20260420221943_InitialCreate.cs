using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace BudgetBuddy.Module.Investments.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.EnsureSchema(
                name: "investments");

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    PreferredLanguage = table.Column<string>(type: "text", nullable: false),
                    DefaultCurrency = table.Column<string>(type: "text", nullable: false),
                    DateFormat = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "text", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRateSnapshots",
                schema: "investments",
                columns: table => new
                {
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Date = table.Column<LocalDate>(type: "date", nullable: false),
                    RateToUsd = table.Column<decimal>(type: "numeric(18,10)", precision: 18, scale: 10, nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRateSnapshots", x => new { x.Currency, x.Date });
                });

            migrationBuilder.CreateTable(
                name: "PriceSnapshots",
                schema: "investments",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Date = table.Column<LocalDate>(type: "date", nullable: false),
                    ClosePrice = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSnapshots", x => new { x.Symbol, x.Date });
                });

            migrationBuilder.CreateTable(
                name: "Investments",
                schema: "investments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PurchaseDate = table.Column<LocalDate>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    SoldDate = table.Column<LocalDate>(type: "date", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Investments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Investments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRateSnapshots_Date",
                schema: "investments",
                table: "ExchangeRateSnapshots",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Investments_AccountId",
                schema: "investments",
                table: "Investments",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Investments_Dedup",
                schema: "investments",
                table: "Investments",
                columns: new[] { "UserId", "Symbol", "PurchaseDate", "Quantity", "PurchasePrice" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Investments_PurchaseDate",
                schema: "investments",
                table: "Investments",
                column: "PurchaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_Investments_UserId_SoldDate",
                schema: "investments",
                table: "Investments",
                columns: new[] { "UserId", "SoldDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Investments_UserId_Symbol",
                schema: "investments",
                table: "Investments",
                columns: new[] { "UserId", "Symbol" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshots_Symbol_Date_Desc",
                schema: "investments",
                table: "PriceSnapshots",
                columns: new[] { "Symbol", "Date" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExchangeRateSnapshots",
                schema: "investments");

            migrationBuilder.DropTable(
                name: "Investments",
                schema: "investments");

            migrationBuilder.DropTable(
                name: "PriceSnapshots",
                schema: "investments");

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "auth");
        }
    }
}
