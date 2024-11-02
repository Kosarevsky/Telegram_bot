using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Services.Services
{
    public class EventPublisher : IEventPublisher
    {
        public event Func<string, List<DateTime>, Task> DatesSaved;
        private readonly ILogger _logger;
        public EventPublisher(ILogger<EventPublisher> logger)
        {
            _logger = logger;
        }
        public async Task PublishDatesSavedAsync(string code, List<DateTime> dates)
        {
            // Добавляем логирование
            _logger.LogInformation($"Publishing dates saved event with code: {code}");
            if (DatesSaved != null)
            {
                await DatesSaved.Invoke(code, dates);
            }
            else
            {
                _logger.LogWarning("No subscribers to DatesSaved event.");
            }
        }
    }
}
