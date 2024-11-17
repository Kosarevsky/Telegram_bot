using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Services.Services
{
    public class EventPublisherService : IEventPublisherService
    {
        public event Func<string, List<DateTime>, List<DateTime>, long?, Task> DatesSaved;
        private readonly ILogger _logger;
        public EventPublisherService(ILogger<EventPublisherService> logger)
        {
            _logger = logger;
        }

        public async Task PublishDatesSavedAsync(string code, List<DateTime> dates, List<DateTime> sendedDates, long? telegramId = null)
        {
            _logger.LogInformation($"Publishing dates saved event with code: {code}");
            if (DatesSaved != null)
            {
                await DatesSaved.Invoke(code, dates, sendedDates, telegramId);
            }
            else
            {
                _logger.LogWarning("No subscribers to DatesSaved event.");
            }
        }
    }
}
