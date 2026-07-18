using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLens.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Replaces the InStock/Reserved/Sold lifecycle with the Open/Deposited/Hold/Sold
    /// workflow, and adds the audit trail behind it.
    /// </summary>
    /// <remarks>
    /// The status column stores the enum as text, so renaming the enum members silently
    /// invalidates every existing row: 'InStock' no longer parses and reading any vehicle
    /// would throw. EF cannot infer a value rename, so the data is remapped here.
    /// </remarks>
    public partial class AddVehicleStatusWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remap stored values to the new member names before anything reads them.
            // Reserved becomes Hold: those vehicles were withheld from sale, and no deposit
            // amount was ever recorded for them, so they cannot become Deposited.
            migrationBuilder.Sql("""
                UPDATE vehicles SET "Status" = 'Open' WHERE "Status" = 'InStock';
                UPDATE vehicles SET "Status" = 'Hold' WHERE "Status" = 'Reserved';
                """);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "vehicles",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalespersonId",
                table: "vehicles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "vehicle_status_changes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DepositAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    SalePrice = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    SalespersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_status_changes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicle_status_changes_salespeople_SalespersonId",
                        column: x => x.SalespersonId,
                        principalTable: "salespeople",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vehicle_status_changes_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_SalespersonId",
                table: "vehicles",
                column: "SalespersonId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_status_changes_CreatedAt",
                table: "vehicle_status_changes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_status_changes_SalespersonId",
                table: "vehicle_status_changes",
                column: "SalespersonId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_status_changes_VehicleId",
                table: "vehicle_status_changes",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_vehicles_salespeople_SalespersonId",
                table: "vehicles",
                column: "SalespersonId",
                principalTable: "salespeople",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vehicles_salespeople_SalespersonId",
                table: "vehicles");

            migrationBuilder.DropTable(
                name: "vehicle_status_changes");

            migrationBuilder.DropIndex(
                name: "IX_vehicles_SalespersonId",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "SalespersonId",
                table: "vehicles");

            // Restore the old member names. Deposited has no pre-workflow equivalent, so it
            // collapses to Reserved, the closest prior meaning.
            migrationBuilder.Sql("""
                UPDATE vehicles SET "Status" = 'InStock' WHERE "Status" = 'Open';
                UPDATE vehicles SET "Status" = 'Reserved' WHERE "Status" IN ('Hold', 'Deposited');
                """);
        }
    }
}
