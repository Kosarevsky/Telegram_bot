using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Notifications.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Notifications.Services
{
    public class TelegramBotService : IHostedService, INotificationService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger _logger;
        private readonly long _chatId;

        public TelegramBotService(ITelegramBotClient botClient, ILogger<TelegramBotService> logger, IConfiguration configuration)
        {
            _botClient = botClient;
            _logger = logger;
            _chatId = long.Parse(configuration["Telegram:ChatId"]);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Telegram Bot...");

            var me = await _botClient.GetMeAsync();
            _logger.LogInformation($"Bot ID: {me.Id}, Bot Name: {me.Username}");

            int offset = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var updates = await _botClient.GetUpdatesAsync(offset: offset, cancellationToken: cancellationToken);

                foreach (var update in updates)
                {
                    if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                    {
                        _logger.LogInformation($"Received update: {update.Message.Text}");
                        await HandleMessage(update.Message);

                        // Увеличиваем offset на 1 после обработки каждого обновления
                        offset = update.Id + 1;
                    }
                }

                // Задержка, чтобы избежать перегрузки запросами
                await Task.Delay(1000, cancellationToken);
            }
        }


        private async Task HandleMessage(Message message)
        {
            _logger.LogInformation($"Received message from {message.Chat.Id}: {message.Text}");

            if (message.Text == "/start")
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Welcome to the bot!");
            }
            else if (message.Text.StartsWith("/date"))
            {
                // Обработайте команду /date
                await _botClient.SendTextMessageAsync(message.Chat.Id, $"Current date: {DateTime.Now}");
            }
            // Добавьте другие команды по мере необходимости
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Telegram Bot...");
            return Task.CompletedTask;
        }
        public async Task SendNotificationAsync(string message)
        {
            await _botClient.SendTextMessageAsync(_chatId, message);
        }

    }
}
