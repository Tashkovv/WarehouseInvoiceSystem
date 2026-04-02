using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseInvoiceSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKaloToPurchaseNoteLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the view that depends on PurchaseNoteLine.Quantity before altering the column type
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_product_purchase_history;");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "PurchaseNoteLine",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "GrossQuantity",
                table: "PurchaseNoteLine",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KaloPercentage",
                table: "PurchaseNoteLine",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Recreate the view (Quantity is now numeric, no CAST needed)
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW vw_product_purchase_history AS

                -- Purchase note lines (individual vendors)
                SELECT
                    pnl.""ProductId"",
                    pn.""PurchaseDate""                                       AS ""Date"",
                    pn.""NoteNumber""                                         AS ""DocumentNumber"",
                    '/purchase-notes/' || pn.""Id""::text                    AS ""DocumentUrl"",
                    (i.""FirstName"" || ' ' || i.""LastName"")               AS ""PartyName"",
                    pn.""WarehouseId"",
                    w.""Name""                                                AS ""WarehouseName"",
                    pnl.""Quantity""                                          AS ""Quantity"",
                    pnl.""UnitPrice"",
                    pnl.""Quantity"" * pnl.""UnitPrice""                     AS ""TotalPrice"",
                    pn.""IndividualId"",
                    NULL::uuid                                                AS ""CompanyId""
                FROM ""PurchaseNoteLine"" pnl
                JOIN ""PurchaseNote"" pn ON pn.""Id"" = pnl.""PurchaseNoteId""
                JOIN ""Individual""   i  ON i.""Id""  = pn.""IndividualId""
                JOIN ""Warehouse""    w  ON w.""Id""  = pn.""WarehouseId""
                WHERE pn.""DeletedOn"" IS NULL
                  AND pn.""Status"" != 4

                UNION ALL

                -- Payable invoice lines (company vendors)
                SELECT
                    il.""ProductId"",
                    inv.""IssueDate""                                         AS ""Date"",
                    inv.""InvoiceNumber""                                     AS ""DocumentNumber"",
                    '/invoices/' || inv.""Id""::text                         AS ""DocumentUrl"",
                    c.""Name""                                                AS ""PartyName"",
                    inv.""WarehouseId"",
                    w.""Name""                                                AS ""WarehouseName"",
                    CAST(il.""Quantity"" AS numeric)                         AS ""Quantity"",
                    il.""UnitPrice"",
                    CAST(il.""Quantity"" AS numeric) * il.""UnitPrice"" * (1 + il.""TaxRate"" / 100.0) AS ""TotalPrice"",
                    NULL::uuid                                                AS ""IndividualId"",
                    inv.""CompanyId""
                FROM ""InvoiceLine"" il
                JOIN ""Invoice""   inv ON inv.""Id"" = il.""InvoiceId""
                JOIN ""Company""   c   ON c.""Id""  = inv.""CompanyId""
                JOIN ""Warehouse"" w   ON w.""Id""  = inv.""WarehouseId""
                WHERE inv.""DeletedOn"" IS NULL
                  AND inv.""Status"" != 6
                  AND inv.""Type""   = 2;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrossQuantity",
                table: "PurchaseNoteLine");

            migrationBuilder.DropColumn(
                name: "KaloPercentage",
                table: "PurchaseNoteLine");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "PurchaseNoteLine",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);
        }
    }
}
