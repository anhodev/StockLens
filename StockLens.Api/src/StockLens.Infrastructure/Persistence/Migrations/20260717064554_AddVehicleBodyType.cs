using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleBodyType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The default must be a valid BodyType name: the column stores the enum as text,
            // so an empty string would fail conversion when EF materialises existing rows.
            migrationBuilder.AddColumn<string>(
                name: "BodyType",
                table: "vehicles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Sedan");

            // Backfill rows that pre-date this column, so existing stock reports its real body
            // style instead of silently collapsing to the Sedan default.
            migrationBuilder.Sql("""
                UPDATE vehicles SET "BodyType" = CASE
                    WHEN "Model" IN ('F-150', 'Silverado', 'Tacoma', 'Ranger', 'Sierra') THEN 'Truck'
                    WHEN "Model" IN ('RAV4', 'CR-V', 'Explorer', 'Escape', 'X5', 'X3', 'Model Y',
                                     'Equinox', 'Tucson', 'Sportage', 'Rogue', 'Highlander',
                                     'Bronco', 'Pilot', 'Tahoe', 'Santa Fe', 'CX-5', 'Telluride',
                                     'Escalade', 'Countryman') THEN 'Suv'
                    WHEN "Model" IN ('Golf GTI', 'Golf', 'Focus ST') THEN 'Hatchback'
                    WHEN "Model" IN ('Pacifica', 'Odyssey', 'Sienna') THEN 'Van'
                    WHEN "Model" IN ('Outback', 'Passport') THEN 'Wagon'
                    WHEN "Model" IN ('Mustang', 'Camaro', 'Challenger') THEN 'Coupe'
                    ELSE 'Sedan'
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_BodyType",
                table: "vehicles",
                column: "BodyType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vehicles_BodyType",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "BodyType",
                table: "vehicles");
        }
    }
}
