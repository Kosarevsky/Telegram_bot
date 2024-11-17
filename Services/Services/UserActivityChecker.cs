using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Services.Services
{
    public class UserActivityChecker : BackgroundService, IUserActivityChecker
    {
        private readonly ILogger<UserActivityChecker> _logger;
        private readonly TimeSpan _timeout = TimeSpan.FromHours(1);
        private readonly IUserService _userService;
        private readonly ITelegramBotService _telegramBotService;

        public UserActivityChecker(ILogger<UserActivityChecker> logger, IUserService userService, ITelegramBotService telegramBotService)
        {
            _logger = logger;
            _userService = userService;
            _telegramBotService = telegramBotService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested) 
            {
                try
                {
                    await CheckInactiveUsers(stoppingToken);
                }
                catch (Exception ex)
                {

                    _logger.LogError(ex, "Error during inactive user check");
                }
                await Task.Delay(_timeout);
            }
        }

        public async Task CheckInactiveUsers(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting inactive user check...");
            var oldDate = DateTime.UtcNow.AddDays(-1);

            var usersToNotify = await _userService.GetAllAsync(u => u.DateLastSubscription <= oldDate && u.IsActive);

            foreach (var user in usersToNotify)
            {
                try
                {
                    var warningMessage = "Мы заметили, что вы давно не проявляли активности. Если хотите продолжать получать уведомления, ответьте /start";
                    await _telegramBotService.SendTextMessage(user.TelegramUserId, warningMessage);
                    _logger.LogInformation($"Warning send to User {user.TelegramUserId} {user.TelegramNickName}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error send warning to user {user.TelegramUserId} {user.TelegramNickName}");
                }

            }
        }
    }
}
