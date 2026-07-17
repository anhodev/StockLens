using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "business_strategies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ScopeKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TargetDaysToSell = table.Column<int>(type: "integer", nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_strategies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Vin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Make = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Trim = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Mileage = table.Column<int>(type: "integer", nullable: false),
                    ListPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AcquiredDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SoldDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sales_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    SoldDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DaysToSell = table.Column<int>(type: "integer", nullable: false),
                    SoldBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_records_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_actions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicle_actions_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_business_strategies_Scope_ScopeKey",
                table: "business_strategies",
                columns: new[] { "Scope", "ScopeKey" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_records_SoldDate",
                table: "sales_records",
                column: "SoldDate");

            migrationBuilder.CreateIndex(
                name: "IX_sales_records_VehicleId",
                table: "sales_records",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_actions_VehicleId",
                table: "vehicle_actions",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_AcquiredDate",
                table: "vehicles",
                column: "AcquiredDate");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_Make",
                table: "vehicles",
                column: "Make");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_Status",
                table: "vehicles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_Vin",
                table: "vehicles",
                column: "Vin",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "business_strategies");

            migrationBuilder.DropTable(
                name: "sales_records");

            migrationBuilder.DropTable(
                name: "vehicle_actions");

            migrationBuilder.DropTable(
                name: "vehicles");
        }
    }
}
