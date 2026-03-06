using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseInvoiceSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FreshMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payment_InvoiceId",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransaction_SourceDocumentId",
                table: "InventoryTransaction");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "InvoiceLine",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_Code",
                table: "Product",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_DeletedOn",
                table: "Product",
                column: "DeletedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Product_IsActive",
                table: "Product",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Name",
                table: "Product",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_InvoiceId_DeletedOn",
                table: "Payment",
                columns: new[] { "InvoiceId", "DeletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_Type",
                table: "Invoice",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_Type_Status_DeletedOn",
                table: "Invoice",
                columns: new[] { "Type", "Status", "DeletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransaction_SourceDocumentId_SourceDocumentType",
                table: "InventoryTransaction",
                columns: new[] { "SourceDocumentId", "SourceDocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_Individual_IsActive",
                table: "Individual",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Product_Code",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_DeletedOn",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_IsActive",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_Name",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Payment_InvoiceId_DeletedOn",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_Type",
                table: "Invoice");

            migrationBuilder.DropIndex(
                name: "IX_Invoice_Type_Status_DeletedOn",
                table: "Invoice");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransaction_SourceDocumentId_SourceDocumentType",
                table: "InventoryTransaction");

            migrationBuilder.DropIndex(
                name: "IX_Individual_IsActive",
                table: "Individual");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "InvoiceLine",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_InvoiceId",
                table: "Payment",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransaction_SourceDocumentId",
                table: "InventoryTransaction",
                column: "SourceDocumentId");
        }
    }
}
