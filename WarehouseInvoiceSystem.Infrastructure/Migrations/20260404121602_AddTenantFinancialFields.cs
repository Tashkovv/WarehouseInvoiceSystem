using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseInvoiceSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantFinancialFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Website",
                table: "Tenant",
                newName: "BankName");

            migrationBuilder.AddColumn<string>(
                name: "BankAccount",
                table: "Tenant",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankBranch",
                table: "Tenant",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Embs",
                table: "Tenant",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "Tenant",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccount",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "BankBranch",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "Embs",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "Tenant");

            migrationBuilder.RenameColumn(
                name: "BankName",
                table: "Tenant",
                newName: "Website");
        }
    }
}
