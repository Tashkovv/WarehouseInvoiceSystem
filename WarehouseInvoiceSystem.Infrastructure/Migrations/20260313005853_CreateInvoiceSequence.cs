using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseInvoiceSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateInvoiceSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Atomic invoice number sequences ──────────────────────────────
            // Replaces the read-then-increment pattern in GenerateInvoiceNumberAsync,
            // which was vulnerable to duplicate numbers under concurrent load.
            // Sequences are session-level in PostgreSQL — no transaction can roll back
            // a nextval(), which is exactly the behaviour we want: even if invoice
            // creation fails, the number is retired, never reused.

            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS invoice_number_seq START 1;");
            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS bill_number_seq START 1;");

            // Seed each sequence to the current highest number so existing invoices
            // are not re-numbered. The subquery returns 0 if no invoices exist yet.
            migrationBuilder.Sql(@"
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

            migrationBuilder.Sql(@"
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop sequences
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS invoice_number_seq;");
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS bill_number_seq;");
        }
    }
}
