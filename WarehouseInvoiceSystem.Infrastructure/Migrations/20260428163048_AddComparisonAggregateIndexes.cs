using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseInvoiceSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComparisonAggregateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PurchaseNote_Status_DeletedOn_PurchaseDate",
                table: "PurchaseNote",
                columns: new[] { "Status", "DeletedOn", "PurchaseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_Type_Status_DeletedOn_IssueDate",
                table: "Invoice",
                columns: new[] { "Type", "Status", "DeletedOn", "IssueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PurchaseNote_Status_DeletedOn_PurchaseDate",
                table: "PurchaseNote");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_Type_Status_DeletedOn_IssueDate",
                table: "Invoice");
        }
    }
}
