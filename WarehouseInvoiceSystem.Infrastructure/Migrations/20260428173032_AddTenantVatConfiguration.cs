using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseInvoiceSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantVatConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VatPayerPeriod",
                table: "Tenant",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<bool>(
                name: "VatRegistered",
                table: "Tenant",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VatRegistrationDate",
                table: "Tenant",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VatPayerPeriod",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "VatRegistered",
                table: "Tenant");

            migrationBuilder.DropColumn(
                name: "VatRegistrationDate",
                table: "Tenant");
        }
    }
}
