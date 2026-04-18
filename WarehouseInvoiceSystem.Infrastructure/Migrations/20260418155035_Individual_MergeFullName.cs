using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseInvoiceSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Individual_MergeFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 0. Drop the view that references FirstName/LastName so column drops don't fail
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_product_purchase_history;");

            // 1. Add FullName with a temporary default so NOT NULL is satisfied
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Individual",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // 2. Populate FullName from the existing split columns
            migrationBuilder.Sql("UPDATE \"Individual\" SET \"FullName\" = \"FirstName\" || ' ' || \"LastName\"");

            // 3. Drop the old columns and index
            migrationBuilder.DropIndex(
                name: "IX_Individual_LastName",
                table: "Individual");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Individual");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Individual");

            // 4. Create index on the new column
            migrationBuilder.CreateIndex(
                name: "IX_Individual_FullName",
                table: "Individual",
                column: "FullName");

            // 5. Recreate the view using FullName
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW vw_product_purchase_history AS

                -- Purchase note lines (individual vendors)
                SELECT
                    pnl.""ProductId"",
                    pn.""PurchaseDate""                                       AS ""Date"",
                    pn.""NoteNumber""                                         AS ""DocumentNumber"",
                    '\purchase-notes\' || pn.""Id""::text                    AS ""DocumentUrl"",
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
                    '\invoices\' || inv.""Id""::text                         AS ""DocumentUrl"",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 0. Drop the FullName-based view so column ops don't fail
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_product_purchase_history;");

            migrationBuilder.DropIndex(
                name: "IX_Individual_FullName",
                table: "Individual");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Individual",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Individual",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Best-effort split: first word → FirstName, remainder → LastName
            migrationBuilder.Sql(
                "UPDATE \"Individual\" SET " +
                "\"FirstName\" = split_part(\"FullName\", ' ', 1), " +
                "\"LastName\" = CASE WHEN strpos(\"FullName\", ' ') > 0 " +
                "              THEN substr(\"FullName\", strpos(\"FullName\", ' ') + 1) " +
                "              ELSE '' END");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Individual");

            migrationBuilder.CreateIndex(
                name: "IX_Individual_LastName",
                table: "Individual",
                column: "LastName");

            // Recreate the original view using FirstName/LastName
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW vw_product_purchase_history AS

                -- Purchase note lines (individual vendors)
                SELECT
                    pnl.""ProductId"",
                    pn.""PurchaseDate""                                       AS ""Date"",
                    pn.""NoteNumber""                                         AS ""DocumentNumber"",
                    '\purchase-notes\' || pn.""Id""::text                    AS ""DocumentUrl"",
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
                  AND pn.""Status"" NOT IN (1, 4)

                UNION ALL

                -- Payable invoice lines (company vendors)
                SELECT
                    il.""ProductId"",
                    inv.""IssueDate""                                         AS ""Date"",
                    inv.""InvoiceNumber""                                     AS ""DocumentNumber"",
                    '\invoices\' || inv.""Id""::text                         AS ""DocumentUrl"",
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
    }
}
