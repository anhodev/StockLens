using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLens.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Moves sales attribution from a free-text <c>SoldBy</c> column onto a real
    /// <c>salespeople</c> table.
    /// </summary>
    /// <remarks>
    /// Ordering matters: the table and a fallback row must exist before the foreign key is
    /// added, otherwise existing sales would point at a salesperson that does not exist and
    /// the migration would fail. The scaffolded version dropped SoldBy first and defaulted
    /// every row to an all-zero GUID, which no salespeople row could satisfy.
    /// </remarks>
    public partial class AddSalespeople : Migration
    {
        /// <summary>
        /// Fallback owner for sales that pre-date the salespeople table. Inactive, so it is
        /// excluded from the sales team and never receives new sales.
        /// </summary>
        private const string UnassignedId = "11111111-1111-1111-1111-111111111111";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "salespeople",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Team = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_salespeople", x => x.Id);
                });

            // Fallback row so pre-existing sales can satisfy the foreign key below.
            migrationBuilder.Sql($"""
                INSERT INTO salespeople ("Id", "FullName", "Email", "Team", "HireDate", "IsActive", "CreatedAt")
                VALUES ('{UnassignedId}', 'Unassigned', NULL, NULL, CURRENT_DATE, FALSE, NOW())
                ON CONFLICT ("Id") DO NOTHING;
                """);

            // Nullable first so existing rows can be backfilled before the constraint lands.
            migrationBuilder.AddColumn<Guid>(
                name: "SalespersonId",
                table: "sales_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql($"""
                UPDATE sales_records SET "SalespersonId" = '{UnassignedId}'
                WHERE "SalespersonId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "SalespersonId",
                table: "sales_records",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_records_SalespersonId",
                table: "sales_records",
                column: "SalespersonId");

            migrationBuilder.CreateIndex(
                name: "IX_salespeople_FullName",
                table: "salespeople",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_salespeople_IsActive",
                table: "salespeople",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_sales_records_salespeople_SalespersonId",
                table: "sales_records",
                column: "SalespersonId",
                principalTable: "salespeople",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Dropped last: the value is superseded by the foreign key above.
            migrationBuilder.DropColumn(
                name: "SoldBy",
                table: "sales_records");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SoldBy",
                table: "sales_records",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "manager");

            migrationBuilder.DropForeignKey(
                name: "FK_sales_records_salespeople_SalespersonId",
                table: "sales_records");

            migrationBuilder.DropIndex(
                name: "IX_sales_records_SalespersonId",
                table: "sales_records");

            migrationBuilder.DropColumn(
                name: "SalespersonId",
                table: "sales_records");

            migrationBuilder.DropTable(
                name: "salespeople");
        }
    }
}
