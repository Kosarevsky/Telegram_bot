using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Notifications.Interfaces;
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types.ReplyMarkups;

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

            var offset = 0;

            var me = await _botClient.GetMeAsync();
            _logger.LogInformation($"Bot ID: {me.Id}, Bot Name: {me.Username}");

            while (!cancellationToken.IsCancellationRequested)
            {
                var updates = await _botClient.GetUpdatesAsync(offset, cancellationToken: cancellationToken);
                foreach (var update in updates)
                {
                    if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                    {
                        _logger.LogInformation($"Received message: {update.Message.Text}");
                        await HandleMessage(update.Message);
                    }
                    else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                    {
                        await HandleCallbackQuery(update.CallbackQuery);
                    }
                    offset = update.Id + 1;
                }
                await Task.Delay(1000, cancellationToken);
            }
        }

        private async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            var selectedCity = callbackQuery.Data;
            _logger.LogInformation($"User selected city: {selectedCity}");

            await _botClient.SendTextMessageAsync(
                callbackQuery.Message.Chat.Id,
                $"Вы выбрали город: {selectedCity}"
            );
        }

        private async Task HandleMessage(Message message)
        {
            _logger.LogInformation($"Received message from {message.Chat.Id}: {message.Text}");

            if (message.Text == "/start")
            {
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new [] // Первый ряд кнопок
                    {
                        InlineKeyboardButton.WithCallbackData("Gdansk", "Gdansk"),
                        InlineKeyboardButton.WithCallbackData("Biala Podlaska", "Biala Podlaska")
                    }
                    // Добавьте другие города аналогичным образом
                });


                await _botClient.SendTextMessageAsync(message.Chat.Id, "Welcome to the bot!");
                
                await _botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Выберите город:",
                    replyMarkup: keyboard
                );


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
