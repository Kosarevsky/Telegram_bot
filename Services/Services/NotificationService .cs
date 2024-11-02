using Microsoft.Extensions.Logging;
using Services.Interfaces;

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
        }

        public async Task OnDatesSavedAsync(string code, List<DateTime> dates)
        {
            await NotificationSend(code, dates);
        }

        public async Task NotificationSend(string code, List<DateTime> dates)
        {


            // Найдите пользователей, выбравших город Biala
            /*            var users = await _context.Users
                            .Where(u => u.SelectedCity == city)
                            .ToListAsync();

                        foreach (var user in users)
                        {
                            foreach (var date in dates)
                            {
                                var message = $"Доступна дата для {city}: {date.ToShortDateString()}";
                                await _telegramBotClient.SendTextMessageAsync(user.ChatId, message);
                            }
                        }*/

            var subscribers = await _userService.GetAllAsync(u => u.Subscriptions.Any(s => s.SubscriptionCode == code));
            var message = GenerateMessage(dates);

            foreach (var subscriber in subscribers)
            {
                await _telegramBotService.SendTextMessage(subscriber.TelegramUserId, message);
                _logger.LogInformation($"Notification sent to user {subscriber.TelegramUserId} for code {code}");
            }
        }

        private string GenerateMessage(List<DateTime> dates)
        {
            return $"Доступные даты: {string.Join(", ", dates.Select(d => d.ToShortDateString()))}";
        }
    }
}
