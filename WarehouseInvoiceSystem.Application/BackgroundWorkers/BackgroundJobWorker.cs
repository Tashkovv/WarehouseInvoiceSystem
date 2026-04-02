namespace WarehouseInvoiceSystem.Application.BackgroundWorkers
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using WarehouseInvoiceSystem.Application.Interfaces;

    /// <summary>
    /// Background worker that executes background jobs on a schedule
    /// Currently configured to run overdue check at 7 AM every day
    /// </summary>
    public partial class BackgroundJobWorker(
        IServiceProvider serviceProvider,
        IAppStateService appState,
        ILicenseService licenseService,
        ILogger<BackgroundJobWorker> logger) : BackgroundService
    {
        private const string LastOverdueCheckKey = "LastOverdueCheckDate";
        private const string LastNotificationCheckKey = "LastNotificationCheckDate";

        private readonly int _overdueCheckHour = 7;
        private readonly int _overdueCheckMinute = 0;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LogScheduled(logger, _overdueCheckHour, _overdueCheckMinute);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await licenseService.ValidateAsync(stoppingToken);

                    if (licenseService.Status is LicenseStatus.Locked or LicenseStatus.NotActivated)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                        continue;
                    }

                    await CheckLicenseExpiryNotificationAsync(stoppingToken);
                    await RunDailyJobsIfDueAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException ex)
                {
                    LogShuttingDown(logger, ex);
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in background job worker");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            logger.LogInformation("Background job worker stopped");
        }

        private async Task CheckLicenseExpiryNotificationAsync(CancellationToken ct)
        {
            if (licenseService.Status != LicenseStatus.Warning
                || !licenseService.GraceDaysRemaining.HasValue)
                return;

            int remaining = licenseService.GraceDaysRemaining.Value;

            using IServiceScope scope = serviceProvider.CreateScope();
            INotificationService notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await notificationService.CreateLicenseExpiringNotificationAsync(remaining, ct);
        }

        private async Task RunDailyJobsIfDueAsync(CancellationToken ct)
        {
            DateTime now = DateTime.UtcNow;
            DateTime todayAtCheckTime = now.Date.AddHours(_overdueCheckHour).AddMinutes(_overdueCheckMinute);

            if (now < todayAtCheckTime)
                return;

            await RunOverdueCheckAsync(now, ct);
            await RunNotificationCheckAsync(now, ct);
        }

        private async Task RunOverdueCheckAsync(DateTime now, CancellationToken ct)
        {
            DateTime? lastCheckDate = await appState.GetDateAsync(LastOverdueCheckKey, ct);
            if (lastCheckDate?.Date == now.Date)
                return;

            LogRunning(logger, now);

            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                IBackgroundJobService backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                List<Guid> overdueIds = await backgroundJobService.CheckAndUpdateOverdueInvoicesAsync();

                if (overdueIds.Count > 0)
                {
                    INotificationService notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    await notificationService.CreateOverdueNotificationAsync(overdueIds, ct);
                }
            }

            await appState.SetDateAsync(LastOverdueCheckKey, now.Date);
            LogCompleted(logger, _overdueCheckHour, _overdueCheckMinute);
        }

        private async Task RunNotificationCheckAsync(DateTime now, CancellationToken ct)
        {
            DateTime? lastNotificationCheck = await appState.GetDateAsync(LastNotificationCheckKey, ct);
            if (lastNotificationCheck?.Date == now.Date)
                return;

            LogNotificationRunning(logger, now);

            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromMinutes(5));
                IBackgroundJobService backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                await backgroundJobService.GenerateAndSendDueDateRemindersAsync(timeoutCts.Token);
            }

            await appState.SetDateAsync(LastNotificationCheckKey, now.Date);
            LogNotificationCompleted(logger);
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Overdue invoice check scheduled for {Hour}:{Minute:00} every day")]
        private static partial void LogScheduled(ILogger logger, int hour, int minute);

        [LoggerMessage(Level = LogLevel.Information, Message = "Running scheduled overdue invoice check at {Timestamp}")]
        private static partial void LogRunning(ILogger logger, DateTime timestamp);

        [LoggerMessage(Level = LogLevel.Information, Message = "Overdue invoice check completed. Next check tomorrow at {Hour}:{Minute:00}")]
        private static partial void LogCompleted(ILogger logger, int hour, int minute);

        [LoggerMessage(Level = LogLevel.Information, Message = "Running scheduled notification check at {Timestamp}")]
        private static partial void LogNotificationRunning(ILogger logger, DateTime timestamp);

        [LoggerMessage(Level = LogLevel.Information, Message = "Notification check completed")]
        private static partial void LogNotificationCompleted(ILogger logger);

        [LoggerMessage(Level = LogLevel.Information, Message = "Background job worker is shutting down")]
        private static partial void LogShuttingDown(ILogger logger, Exception ex);
    }
}
