using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseInvoiceSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateProductPurchaseHistoryView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // vw_product_purchase_history
            // UNIONs purchase-note lines (individual vendors) and payable invoice lines
            // (company vendors) into a single queryable shape so the purchased transaction
            // history can be filtered, sorted, and paginated entirely in SQL.
            //
            // Enum values used:
            //   PurchaseNoteStatus.Cancelled = 4
            //   InvoiceStatus.Cancelled      = 6
            //   InvoiceType.Payable          = 2

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
                    CAST(pnl.""Quantity"" AS numeric)                        AS ""Quantity"",
                    pnl.""UnitPrice"",
                    CAST(pnl.""Quantity"" AS numeric) * pnl.""UnitPrice""    AS ""TotalPrice"",
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
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_product_purchase_history;");
        }
    }
}
