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

            _logger.LogWarning("* NotificationService initialized.");
        }

        public async Task OnDatesSavedAsync(string code, List<DateTime> dates, List<DateTime> previouslySentDates, long? telegramId = null)
        {
            await NotificationSend(code, dates, previouslySentDates, telegramId);
        }

        public async Task NotificationSend(string code, List<DateTime> dates, List<DateTime> previouslySentDates, long? telegramId = null)
        {
            try
            {
                var newDates = dates.Except(previouslySentDates).ToList();
                var missingDates = previouslySentDates.Except(dates).ToList();
                var message = GenerateMessage(dates, newDates, missingDates, code);

                if (newDates.Any() || missingDates.Any())
                {
                    var users = telegramId == null
                        ? await _userService.GetAllAsync(u => u.IsActive && u.Subscriptions.Any(s => s.SubscriptionCode == code))
                        : await _userService.GetAllAsync(u => u.IsActive && u.TelegramUserId == telegramId && u.Subscriptions.Any(c => c.SubscriptionCode == code));

                    var tasks = new List<Task>();

                    foreach (var user in users)
                    {
                        var telegramUserId = user.TelegramUserId;
                        tasks.Add(SendNotification(telegramUserId, message, code));
                    }

                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending notifications");
            }
        }

        private async Task SendNotification(long telegramUserId, string message, string code)
        {
            try
            {
                await _telegramBotService.SendTextMessage(telegramUserId, message);
                _logger.LogInformation($"Notification sent to user {telegramUserId} for code {code}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to send notification to user {telegramUserId} for code {code}");
            }
        }

        private string GenerateMessage(List<DateTime> dates, List<DateTime> newDates, List<DateTime> missingDates, string code)
        {
            var siteIdentifier = CodeMapping.GetSiteIdentifierByCode(code);
            var truncatedText = _bezKolejkiService.TruncateText(CodeMapping.GetKeyByCode(code), 30);
            var url = CodeMapping.GetUrlByCode(code);

            var message = $"{siteIdentifier}. {truncatedText}\n";
            var sortedDates = dates.OrderBy(d => d).ToList();
            var sortedNewDates = newDates.OrderBy(d => d).ToList();
            var sortedMissingDates = missingDates.OrderBy(d => d).ToList();

            if (sortedDates.SequenceEqual(sortedNewDates))
            {
                return message + (sortedDates.Any()
                    ? $"Доступны даты: {string.Join(", ", sortedDates.Select(d => d.ToShortDateString()))}\n{url}"
                    : "Нет дат.");
            }

            if (sortedNewDates.Any())
            {
                message += $"Новая дата: {string.Join(", ", sortedNewDates.Select(d => d.ToShortDateString()))}\n";
            }

            if (sortedMissingDates.Any())
            {
                message += $"Разобрали дату: {string.Join(", ", sortedMissingDates.Select(d => d.ToShortDateString()))}\n";
            }

            message += sortedDates.Any()
                ? $"Доступны даты: {string.Join(", ", sortedDates.Select(d => d.ToShortDateString()))}\n{url}"
                : "Нет дат.";

            return message;
        }
    }
}
