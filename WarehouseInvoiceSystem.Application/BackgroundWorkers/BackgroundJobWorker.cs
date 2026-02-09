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
        ILogger<BackgroundJobWorker> logger) : BackgroundService
    {

        // Time when overdue check should run (7 AM)
        private readonly int _overdueCheckHour = 7;  // 24-hour format (7 = 7 AM)
        private readonly int _overdueCheckMinute = 0; // 0 minutes

        private DateTime _lastOverdueCheckDate = DateTime.MinValue;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Background job worker started");
            logger.LogInformation("Overdue invoice check scheduled for {Hour}:{Minute:00} every day",
                _overdueCheckHour, _overdueCheckMinute);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Get current time
                    DateTime now = DateTime.UtcNow;
                    DateTime todayAtCheckTime = now.Date.AddHours(_overdueCheckHour).AddMinutes(_overdueCheckMinute);

                    // Check if it's time to run the overdue check
                    // Only run once per day at the specified time
                    if (now >= todayAtCheckTime && _lastOverdueCheckDate != now.Date)
                    {
                        logger.LogInformation("Running scheduled overdue invoice check at {Timestamp}", now);

                        using (IServiceScope scope = serviceProvider.CreateScope())
                        {
                            var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                            await backgroundJobService.CheckAndUpdateOverdueInvoicesAsync();
                        }

                        _lastOverdueCheckDate = now.Date;
                        logger.LogInformation("Overdue invoice check completed. Next check tomorrow at {Hour}:{Minute:00}",
                            _overdueCheckHour, _overdueCheckMinute);
                    }

                    // Run check every 1 minute to see if it's time to execute scheduled jobs
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Background job worker is shutting down");
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
