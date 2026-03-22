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
        ILogger<BackgroundJobWorker> logger) : BackgroundService
    {
        // Keys used to persist last-check dates across restarts (UTC date, ISO 8601)
        private const string LastOverdueCheckKey = "LastOverdueCheckDate";
        private const string LastNotificationCheckKey = "LastNotificationCheckDate";

        // Time when overdue check should run (7 AM UTC)
        private readonly int _overdueCheckHour = 7;  // 24-hour format (7 = 7 AM)
        private readonly int _overdueCheckMinute = 0; // 0 minutes

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LogScheduled(logger, _overdueCheckHour, _overdueCheckMinute);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // All timestamps are UTC
                    DateTime now = DateTime.UtcNow;
                    DateTime todayAtCheckTime = now.Date.AddHours(_overdueCheckHour).AddMinutes(_overdueCheckMinute);

                    // Read persisted date so restarts don't re-run the check on the same day
                    DateTime? lastCheckDate = await appState.GetDateAsync(LastOverdueCheckKey, stoppingToken);

                    // Check if it's time to run the overdue check
                    // Only run once per day at the specified time
                    if (now >= todayAtCheckTime && lastCheckDate?.Date != now.Date)
                    {
                        LogRunning(logger, now);

                        using (IServiceScope scope = serviceProvider.CreateScope())
                        {
                            var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                            await backgroundJobService.CheckAndUpdateOverdueInvoicesAsync();
                        }

                        await appState.SetDateAsync(LastOverdueCheckKey, now.Date);
                        LogCompleted(logger, _overdueCheckHour, _overdueCheckMinute);
                    }

                    // ── Notification check (runs after overdue check so newly-overdue invoices are excluded) ──
                    DateTime? lastNotificationCheck = await appState.GetDateAsync(LastNotificationCheckKey, stoppingToken);
                    if (now >= todayAtCheckTime && lastNotificationCheck?.Date != now.Date)
                    {
                        LogNotificationRunning(logger, now);

                        using (IServiceScope scope = serviceProvider.CreateScope())
                        {
                            var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                            await backgroundJobService.GenerateAndSendDueDateRemindersAsync(stoppingToken);
                        }

                        await appState.SetDateAsync(LastNotificationCheckKey, now.Date);
                        LogNotificationCompleted(logger);
                    }

                    // Run check every 1 minute to see if it's time to execute scheduled jobs
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
                    // Continue running despite errors
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            logger.LogInformation("Background job worker stopped");
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
