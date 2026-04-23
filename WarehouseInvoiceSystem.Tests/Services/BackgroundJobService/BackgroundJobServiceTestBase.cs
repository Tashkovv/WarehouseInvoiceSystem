namespace WarehouseInvoiceSystem.Tests.Services.BackgroundJobService;

using NSubstitute;
using WarehouseInvoiceSystem.Application.Interfaces;
using WarehouseInvoiceSystem.Application.Services;
using WarehouseInvoiceSystem.Domain.Interfaces;

public abstract class BackgroundJobServiceTestBase
{
    protected readonly IInvoiceRepository InvoiceRepo = Substitute.For<IInvoiceRepository>();
    protected readonly IInvoiceService InvoiceSvc = Substitute.For<IInvoiceService>();
    protected readonly INotificationService NotificationSvc = Substitute.For<INotificationService>();

    protected BackgroundJobService CreateService() => new(InvoiceRepo, InvoiceSvc, NotificationSvc);
}
