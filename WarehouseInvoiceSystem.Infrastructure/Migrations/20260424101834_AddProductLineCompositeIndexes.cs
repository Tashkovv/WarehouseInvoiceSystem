using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseInvoiceSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductLineCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PurchaseNoteLine_ProductId_PurchaseNoteId",
                table: "PurchaseNoteLine",
                columns: new[] { "ProductId", "PurchaseNoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLine_ProductId_InvoiceId",
                table: "InvoiceLine",
                columns: new[] { "ProductId", "InvoiceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PurchaseNoteLine_ProductId_PurchaseNoteId",
                table: "PurchaseNoteLine");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceLine_ProductId_InvoiceId",
                table: "InvoiceLine");
        }
    }
}
