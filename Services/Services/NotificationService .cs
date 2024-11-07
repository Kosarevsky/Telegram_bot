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
        private readonly IBezKolejkiService _bezKolejkiService;

        public NotificationService(ILogger<NotificationService> logger,
                              ITelegramBotService telegramBotService,
                              IEventPublisherService eventPublisher,
                              IUserService userService,
                              IBezKolejkiService bezKolejkiService
            )
        {
            _logger = logger;
            _telegramBotService = telegramBotService;
            _userService = userService;
            _bezKolejkiService = bezKolejkiService;

            //eventPublisher.DatesSaved += OnDatesSavedAsync;
            _logger.LogWarning("* NotificationService initialized.");
        }

        public async Task OnDatesSavedAsync(string code, List<DateTime> dates, List<DateTime> sendedDates)
        {
            await NotificationSend(code, dates, sendedDates);
        }

        public async Task NotificationSend(string code, List<DateTime> dates, List<DateTime> sendedDates)
        {
            var users = await _userService.GetAllAsync(u => u.Subscriptions.Any(s => s.SubscriptionCode == code));

            foreach (var user in users)
            {
                var newDates = dates.Except(sendedDates).ToList();
                var missingDates = sendedDates.Except(dates).ToList();

                if (newDates.Any() || missingDates.Any())
                {
                    var message = GenerateMessage(newDates, missingDates, code);
                    try
                    {
                        await _telegramBotService.SendTextMessage(user.TelegramUserId, message, code, dates);
                        _logger.LogInformation($"Notification sent to user {user.TelegramUserId} for code {code}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to send notification to user {user.TelegramUserId} for code {code}");
                    }
                }
            }
        }

        private string GenerateMessage(List<DateTime> newDates , List<DateTime> missingDates,  string code)
        {
            var message = $"{CodeMapping.GetSiteIdentifierByCode(code)}. {CodeMapping.GetKeyByCode(code)}\n";

            if (newDates.Any()) {
                message += $"Новая дата: {string.Join(", ", newDates.Select(d => d.ToShortDateString()))}";  
            }

            if (missingDates.Any()) {
                message += $"Разобрали дату: {string.Join(", ", missingDates.Select(d=>d.ToShortDateString()))}";
            }

            return message;
        }
    }
}
