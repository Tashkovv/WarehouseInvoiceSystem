using WarehouseInvoiceSystem.BlazorUI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using System.Globalization;
using System.Security.Claims;
using WarehouseInvoiceSystem.Application.BackgroundWorkers;
using WarehouseInvoiceSystem.Application.DTOs.User;
using WarehouseInvoiceSystem.Application.Interfaces;
using WarehouseInvoiceSystem.Application.Services;
using WarehouseInvoiceSystem.Application;
using WarehouseInvoiceSystem.BlazorUI.Components;
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
builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

// Register Settings
builder.Services.AddApplicationSettings(builder.Configuration);

// Add MudBlazor services
builder.Services.AddMudServices();
builder.Services.AddScoped<MudBlazor.MudLocalizer, WarehouseInvoiceSystem.BlazorUI.Localization.CustomMudLocalizer>();

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
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddTransient<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<WisDialogService>();

// Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/api/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<WisAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<WisAuthenticationStateProvider>());

// Background job infrastructure
builder.Services.AddSingleton<IAppStateService, AppStateService>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
builder.Services.AddHostedService<BackgroundJobWorker>();

// License services (Windows-only — app runs as NSSM service on client PCs)
#pragma warning disable CA1416
builder.Services.AddSingleton<IHardwareIdService, HardwareIdService>();
#pragma warning restore CA1416
builder.Services.AddSingleton<ILicenseService, LicenseService>();

WebApplication app = builder.Build();

// Seed database if --seed flag is passed
if (args.Contains("--seed"))
{
    var dbFactory = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    await SeedData.RunAsync(dbFactory);
    return;
}

// Validate license on startup
var licenseService = app.Services.GetRequiredService<ILicenseService>();
await licenseService.ValidateAsync();

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
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Auth endpoints (cookie auth requires HTTP response, not SignalR)
app.MapPost("/api/auth/login", async (HttpContext httpContext, IUserService userService) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    string username = form["username"].ToString();
    string password = form["password"].ToString();

    LoginResultDto result = await userService.LoginAsync(new LoginDto { Username = username, Password = password });

    if (!result.Success)
        return Results.Redirect($"/login?error={result.ErrorMessage}");

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, result.User!.Id.ToString()),
        new(ClaimTypes.Name, result.User.Username),
        new(ClaimTypes.Email, result.User.Email),
        new(ClaimTypes.Role, result.User.Role.ToString())
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties { IsPersistent = form.ContainsKey("remember") });

    return Results.Redirect("/");
});

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

await app.RunAsync();