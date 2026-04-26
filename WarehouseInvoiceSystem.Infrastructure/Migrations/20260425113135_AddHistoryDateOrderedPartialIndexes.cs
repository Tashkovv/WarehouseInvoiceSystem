using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseInvoiceSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoryDateOrderedPartialIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Receivable invoices, date-ordered. Used by sold-history pagination.
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Invoice_Receivable_Active_IssueDate""
                ON ""Invoice"" (""IssueDate"" DESC)
                WHERE ""DeletedOn"" IS NULL AND ""Type"" = 1 AND ""Status"" NOT IN (1, 6);
            ");

            // Payable invoices, date-ordered. Used by purchased-history view (payable branch).
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Invoice_Payable_Active_IssueDate""
                ON ""Invoice"" (""IssueDate"" DESC)
                WHERE ""DeletedOn"" IS NULL AND ""Type"" = 2 AND ""Status"" NOT IN (1, 6);
            ");

            // Active purchase notes, date-ordered. Used by purchased-history view (PN branch).
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_PurchaseNote_Active_PurchaseDate""
                ON ""PurchaseNote"" (""PurchaseDate"" DESC)
                WHERE ""DeletedOn"" IS NULL AND ""Status"" NOT IN (1, 4);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_PurchaseNote_Active_PurchaseDate"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Invoice_Payable_Active_IssueDate"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Invoice_Receivable_Active_IssueDate"";");
        }
    }
}
