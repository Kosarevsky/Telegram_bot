using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BezKolejki_bot.Interfaces;
using Services.Interfaces;

namespace BezKolejki_bot.Services
{
    public class ScheduledTaskService : IHostedService, IDisposable, IScheduledTaskService
    {
        private readonly ILogger<ScheduledTaskService> _logger;
        private readonly IBrowserAutomationService _browserAutomationService;
        private readonly INotificationService _notificationService;
        private Timer _timer = null!;
        private readonly int _interval;
        public ScheduledTaskService(
            ILogger<ScheduledTaskService> logger, 
            IBrowserAutomationService browserAutomationService, 
            IConfiguration configuration,
            INotificationService notificationService)
        {
            _logger = logger;
            _browserAutomationService = browserAutomationService;
            _notificationService = notificationService;

            var bialaTaskScheduled = configuration["ScheduledTask:Biala"];
            if (!int.TryParse(bialaTaskScheduled, out _interval) || _interval <= 0)
            {
                throw new InvalidOperationException("Biala Task Scheduled interval is not properly configured.");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ScheduledTaskService is starting.");
            _timer = new Timer(ExecuteTaskAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(_interval));
            _logger.LogInformation("Timer is set to interval: {interval} seconds.", _interval);
            return Task.CompletedTask;
        }

        private async void ExecuteTaskAsync(object? state)
        {
            await Task.Run(async () =>
            {
                _logger.LogInformation("Executing scheduled task");
                try
                {
                    await _browserAutomationService.GetAvailableDateAsync("https://bezkolejki.eu/luwbb/");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error executing scheduled task: {ex.Message}");
                }
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"ScheduledTaskService is Stopping");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
