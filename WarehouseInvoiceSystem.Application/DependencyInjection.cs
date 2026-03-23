using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WarehouseInvoiceSystem.Application.Settings;

namespace WarehouseInvoiceSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<EmailSettings>()
                .Bind(configuration.GetSection("EmailSettings"))
                .ValidateOnStart();

        services.AddOptions<EncryptionSettings>()
                .Bind(configuration.GetSection("EncryptionSettings"))
                .ValidateOnStart();

        services.AddOptions<NotificationSettings>()
                .Bind(configuration.GetSection("NotificationSettings"))
                .ValidateOnStart();

        return services;
    }
}
