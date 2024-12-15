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
                    var warningThresholdDate = DateTime.UtcNow.AddDays(-7);
                    var DeactivationThresholdDate = warningThresholdDate.AddDays(-1);
                    await CheckInactiveUsers(warningThresholdDate, DeactivationThresholdDate, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during inactive user check");
                }
                await Task.Delay(_timeout);
            }
        }

        public async Task CheckInactiveUsers(DateTime warningThresholdDate, DateTime DeactivationThresholdDate, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting inactive user check...");

            var usersToCheck = await _userService.GetAllAsync(u => u.DateLastSubscription <= warningThresholdDate && u.IsActive);

            foreach (var user in usersToCheck)
            {
                try
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    if (user.DateLastSubscription > DeactivationThresholdDate)
                    {
                        var warningMessage = "Мы заметили, что вы давно не проявляли активности. Если хотите продолжать получать уведомления, ответьте /start";
                        await _telegramBotService.SendTextMessage(user.TelegramUserId, warningMessage);
                        _logger.LogInformation($"Warning send to User {user.TelegramUserId} {user.UserName}");
                    }
                    else
                    {
                        await _userService.DeactivateUserAsync(user.TelegramUserId);
                        var DeactivationMessage = "Рассылка сообщений остановлена.";
                        await _telegramBotService.SendTextMessage(user.TelegramUserId, DeactivationMessage);
                        _logger.LogInformation($"User {user.TelegramUserId} {user.UserName} marked as inactive");
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing user {user.TelegramUserId} {user.UserName}");
                }
            }
        }
    }
}
