namespace WarehouseInvoiceSystem.Infrastructure.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    internal static class MigrationHelper
    {
        internal static void Up(MigrationBuilder mb)
        {
            mb.Sql("CREATE SEQUENCE IF NOT EXISTS invoice_number_seq START 1;");
            mb.Sql("CREATE SEQUENCE IF NOT EXISTS bill_number_seq START 1;");

            mb.Sql(@"
                SELECT setval(
                    'invoice_number_seq',
                    GREATEST(
                        (SELECT COALESCE(MAX(CAST(SUBSTRING(""InvoiceNumber"" FROM 13) AS INTEGER)), 0)
                         FROM ""Invoice""
                         WHERE ""InvoiceNumber"" LIKE 'INV-%'),
                        1
                    )
                );
            ");

            mb.Sql(@"
                SELECT setval(
                    'bill_number_seq',
                    GREATEST(
                        (SELECT COALESCE(MAX(CAST(SUBSTRING(""InvoiceNumber"" FROM 14) AS INTEGER)), 0)
                         FROM ""Invoice""
                         WHERE ""InvoiceNumber"" LIKE 'BILL-%'),
                        1
                    )
                );
            ");

            mb.Sql(@"
                CREATE OR REPLACE VIEW vw_product_purchase_history AS

                -- Purchase note lines (individual vendors)
                SELECT
                    pnl.""ProductId"",
                    pn.""PurchaseDate""                                       AS ""Date"",
                    pn.""NoteNumber""                                         AS ""DocumentNumber"",
                    '/purchase-notes/' || pn.""Id""::text                    AS ""DocumentUrl"",
                    i.""FullName""                                            AS ""PartyName"",
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
                  AND pn.""Status"" NOT IN (1, 4)

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
                  AND inv.""Status"" NOT IN (1, 6)
                  AND inv.""Type""   = 2;
            ");
        }

        internal static void Down(MigrationBuilder mb)
        {
            mb.Sql("DROP VIEW IF EXISTS vw_product_purchase_history;");
            mb.Sql("DROP SEQUENCE IF EXISTS invoice_number_seq;");
            mb.Sql("DROP SEQUENCE IF EXISTS bill_number_seq;");
        }
    }
}
