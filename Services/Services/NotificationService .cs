using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Services.Models;

namespace Services.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly IUserService _userService;
        private readonly ITelegramBotService _telegramBotService;

        public NotificationService(ILogger<NotificationService> logger,
                              ITelegramBotService telegramBotService,
                              IEventPublisher eventPublisher,
                              IUserService userService)
        {
            _logger = logger;
            _telegramBotService = telegramBotService;
            _userService = userService;

            //eventPublisher.DatesSaved += OnDatesSavedAsync;
            _logger.LogWarning("***************** NotificationService initialized.");
        }

        public async Task OnDatesSavedAsync(string code, List<DateTime> dates)
        {
            await NotificationSend(code, dates);
        }

        public async Task NotificationSend(string code, List<DateTime> dates)
        {
            var subscribers = await _userService.GetAllAsync(u => u.Subscriptions.Any(s => s.SubscriptionCode == code));

            foreach (var subscriber in subscribers)
            {
                var subscriptions = subscriber.Subscriptions.Where(s=> string.Equals(code, s.SubscriptionCode));

                foreach (var subscription in subscriptions)
                {
                    var sendedDates = subscription.UserSubscriptionItems.Select(x=> x.AvailableDate);
                    var newDates = dates.Except(sendedDates).ToList();
                    var missingDates = sendedDates.Except(dates).ToList();


                    if (newDates.Any() || missingDates.Any()) { 
                        var message = GenerateMessage( newDates, missingDates, code);
                        await _telegramBotService.SendTextMessage(subscriber.TelegramUserId, message, code, dates);
                        _logger.LogInformation($"Notification sent to user {subscriber.TelegramUserId} for code {code}");
                    }

                }
            }
        }

        private string GenerateMessage(List<DateTime> newDates , List<DateTime> missingDates,  string code)
        {
            var message = BialaCodeMapping.buttonCodeMapping
                 .FirstOrDefault(el => string.Equals(el.Value, code)).Key ?? string.Empty;

            if (newDates.Any()) {
                message += $" Новая дата: {string.Join(", ", newDates.Select(d => d.ToShortDateString()))}";  
            }

            if (missingDates.Any()) {
                message += $" Разобрали дату: {string.Join(", ", missingDates.Select(d=>d.ToShortDateString()))}";
            }

            return message;
        }
    }
}
