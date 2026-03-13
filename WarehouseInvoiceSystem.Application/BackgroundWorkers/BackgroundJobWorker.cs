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
    public class BackgroundJobWorker(
        IServiceProvider serviceProvider,
        IAppStateService appState,
        ILogger<BackgroundJobWorker> logger) : BackgroundService
    {
        // Key used to persist the last overdue-check date across restarts (UTC date, ISO 8601)
        private const string LastOverdueCheckKey = "LastOverdueCheckDate";

        // Time when overdue check should run (7 AM UTC)
        private readonly int _overdueCheckHour = 7;  // 24-hour format (7 = 7 AM)
        private readonly int _overdueCheckMinute = 0; // 0 minutes

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Overdue invoice check scheduled for {Hour}:{Minute:00} every day",
                _overdueCheckHour, _overdueCheckMinute);

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
                        logger.LogInformation("Running scheduled overdue invoice check at {Timestamp}", now);

                        using (IServiceScope scope = serviceProvider.CreateScope())
                        {
                            var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                            await backgroundJobService.CheckAndUpdateOverdueInvoicesAsync();
                        }

                        await appState.SetDateAsync(LastOverdueCheckKey, now.Date);
                        logger.LogInformation("Overdue invoice check completed. Next check tomorrow at {Hour}:{Minute:00}",
                            _overdueCheckHour, _overdueCheckMinute);
                    }

                    // Run check every 1 minute to see if it's time to execute scheduled jobs
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException ex)
                {
                    logger.LogInformation($"Background job worker is shutting down with exception: {ex.ToString()}");
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
    }
}