using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseInvoiceSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_DeletedOn",
                table: "Warehouse",
                column: "DeletedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_IsActive",
                table: "Warehouse",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouse_IsDefault",
                table: "Warehouse",
                column: "IsDefault");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Warehouse_DeletedOn",
                table: "Warehouse");

            migrationBuilder.DropIndex(
                name: "IX_Warehouse_IsActive",
                table: "Warehouse");

            migrationBuilder.DropIndex(
                name: "IX_Warehouse_IsDefault",
                table: "Warehouse");
        }
    }
}
