﻿using Data.Context;
using Data.Interfaces;
using Data.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotifyKP_bot.Interfaces;
using Services.Interfaces;

namespace NotifyKP_bot.Services
{
    public class ScheduledTaskService : IHostedService, IDisposable, IScheduledTaskService
    {
        private readonly ILogger<ScheduledTaskService> _logger;
        private readonly IBrowserAutomationService _browserAutomationService;
        private readonly IOperationRecordService _operationRecordService;
        private Timer _timer;
        private readonly int _interval;
        public ScheduledTaskService(
            ILogger<ScheduledTaskService> logger, 
            IBrowserAutomationService browserAutomationService, 
            IOperationRecordService operationRecordService,
            IConfiguration configuration)
        {
            _logger = logger;
            _browserAutomationService = browserAutomationService;
            _operationRecordService = operationRecordService;

            var bialaTaskScheduled = configuration["ScheduledTask:Biala"];
            if (!int.TryParse(bialaTaskScheduled, out _interval) || _interval <= 0)
            {
                throw new InvalidOperationException("Biala Task Scheduled interval is not properly configured.");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ScheduledTaskService is starting.");
            _timer = new Timer(ExecuteTask, null, TimeSpan.Zero, TimeSpan.FromSeconds(_interval));
            _logger.LogInformation("Timer is set to interval: {interval} seconds.", _interval);
            return Task.CompletedTask;
        }

        private async void ExecuteTask(object? state)
        {
            _logger.LogInformation("Executing scheduled task");
            try
            {
                var dates = await _browserAutomationService.GetAvailableDateAsync("https://bezkolejki.eu/luwbb/");
                if (dates != null && dates.Any())
                {
                    _operationRecordService.SaveOperationDate(dates);
                    _logger.LogInformation("Dates saved Successfully");
                }
                else {
                    _logger.LogInformation("no dates available to save");
                }
             
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing scheduled task: {ex.Message}");
            }
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
