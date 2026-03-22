using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WarehouseInvoiceSystem.Application.BackgroundWorkers;
using WarehouseInvoiceSystem.Application.Interfaces;
using WarehouseInvoiceSystem.Application.Services;
using WarehouseInvoiceSystem.Application.Settings;
using WarehouseInvoiceSystem.Domain.Interfaces;
using WarehouseInvoiceSystem.Infrastructure.Common;
using WarehouseInvoiceSystem.Infrastructure.Data;
using WarehouseInvoiceSystem.Infrastructure.Repositories;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Culture setup
CultureInfo mkdCulture = new("mk-MK");
mkdCulture.NumberFormat.CurrencySymbol = "ден.";
mkdCulture.NumberFormat.CurrencyDecimalDigits = 2;
mkdCulture.NumberFormat.CurrencyPositivePattern = 3;

CultureInfo.DefaultThreadCurrentCulture = mkdCulture;
CultureInfo.DefaultThreadCurrentUICulture = mkdCulture;

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Settings
builder.Services.AddOptions<EmailSettings>()
                .Bind(builder.Configuration.GetSection("EmailSettings"))
                .ValidateOnStart();

builder.Services.Configure<EncryptionSettings>(
    builder.Configuration.GetSection("EncryptionSettings"));

builder.Services.Configure<NotificationSettings>(
    builder.Configuration.GetSection("NotificationSettings"));

// Add Database Context
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repositories
builder.Services.AddTransient<ICompanyRepository, CompanyRepository>();
builder.Services.AddTransient<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddTransient<IPaymentRepository, PaymentRepository>();
builder.Services.AddTransient<IProductRepository, ProductRepository>();
builder.Services.AddTransient<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddTransient<IStockLevelRepository, StockLevelRepository>();
builder.Services.AddTransient<IInventoryTransactionRepository, InventoryTransactionRepository>();
builder.Services.AddTransient<IIndividualRepository, IndividualRepository>();
builder.Services.AddTransient<IPurchaseNoteRepository, PurchaseNoteRepository>();
builder.Services.AddTransient<ITenantRepository, TenantRepository>();
builder.Services.AddTransient<INotificationRepository, NotificationRepository>();

// Register Services
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IIndividualService, IndividualService>();
builder.Services.AddScoped<IPurchaseNoteService, PurchaseNoteService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

// Background job infrastructure
builder.Services.AddSingleton<IAppStateService, AppStateService>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
builder.Services.AddHostedService<BackgroundJobWorker>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
