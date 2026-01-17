# Warehouse Invoice System

Invoice and payment tracking system for warehouse operations.

## Setup

### Database Configuration

1. Copy `appsettings.Example.json` to `appsettings.Development.json`
2. Update the connection string with your PostgreSQL credentials
3. Run migrations: `dotnet ef database update --project WarehouseInvoiceSystem.Infrastructure --startup-project WarehouseInvoiceSystem.API`

### User Secrets (Recommended)

Instead of using `appsettings.Development.json`, use user secrets:
```bash
cd WarehouseInvoiceSystem.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=warehouse_invoices;Username=postgres;Password=postgres"
```

## Running the Application
```bash
# Run API
dotnet run --project WarehouseInvoiceSystem.API

# Run Blazor UI
dotnet run --project WarehouseInvoiceSystem.BlazorUI
```