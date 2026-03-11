using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseInvoiceSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WarehouseIsActiveColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Warehouse",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Warehouse");
        }
    }
}
