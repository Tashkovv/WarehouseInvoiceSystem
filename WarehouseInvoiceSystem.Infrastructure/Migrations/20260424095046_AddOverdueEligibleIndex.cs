using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseInvoiceSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOverdueEligibleIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Invoice_DueDate_OverdueEligible",
                table: "Invoice",
                column: "DueDate",
                filter: "\"Status\" IN (2, 3) AND \"DeletedOn\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoice_DueDate_OverdueEligible",
                table: "Invoice");
        }
    }
}
