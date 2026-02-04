using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using System.Globalization;
using WarehouseInvoiceSystem.Application.Interfaces;
using WarehouseInvoiceSystem.Application.Services;
using WarehouseInvoiceSystem.BlazorUI.Components;
using WarehouseInvoiceSystem.Domain.Interfaces;
using WarehouseInvoiceSystem.Infrastructure.Data;
using WarehouseInvoiceSystem.Infrastructure.Repositories;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Culture setup 
CultureInfo mkdCulture = new("mk-MK");
mkdCulture.NumberFormat.CurrencySymbol = "MKD";
mkdCulture.NumberFormat.CurrencyDecimalDigits = 2;
mkdCulture.NumberFormat.CurrencyPositivePattern = 3;

CultureInfo.DefaultThreadCurrentCulture = mkdCulture;
CultureInfo.DefaultThreadCurrentUICulture = mkdCulture;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Localization service
builder.Services.AddScoped<ILocalizationService, LocalizationService>();

// Add Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repositories
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

// Register Services
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
