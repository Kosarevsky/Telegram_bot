using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types.ReplyMarkups;
using Services.Models;


namespace Services.Services
{
    public class TelegramBotService : IHostedService, ITelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger _logger;
        private readonly long _chatId;
        private readonly IUserService _userService;
        public TelegramBotService(
            ITelegramBotClient botClient,
            ILogger<TelegramBotService> logger,
            IConfiguration configuration,
            IUserService userService)
        {
            _botClient = botClient;
            _logger = logger;
            _chatId = long.Parse(configuration["Telegram:ChatId"]);
            _userService = userService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("Starting Telegram Bot...");

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
                        if (update?.Message?.Text != null) { 
                            _logger.LogInformation($"Received message: {update.Message.Text}");
                            await HandleMessage(update.Message);
                        }
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
            var selectedButton = callbackQuery.Data;
            var userId = callbackQuery.From.Id;
            _logger.LogInformation($"User selected city: {selectedButton}");

            switch (selectedButton)
            {
                case "Biala Podlaska": 
                    var questionKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new [] { InlineKeyboardButton.WithCallbackData("Karta Polaka - dorośli", "/Biala01") },
                        new [] { InlineKeyboardButton.WithCallbackData("Karta Polaka - dzieci", "/Biala02") },
                        new [] { InlineKeyboardButton.WithCallbackData("Pobyt czasowy - wniosek", "/Biala03") },
                        new [] { InlineKeyboardButton.WithCallbackData("Pobyt czasowy - braki formalne", "/Biala04") },
                        new [] { InlineKeyboardButton.WithCallbackData("Pobyt czasowy - odbiór karty", "/Biala05") },
                        new [] { InlineKeyboardButton.WithCallbackData("Pobyt stały i rezydent - wniosek", "/Biala06") },
                        new [] { InlineKeyboardButton.WithCallbackData("Pobyt stały i rezydent - braki formalne", "/Biala07") },
                        new [] { InlineKeyboardButton.WithCallbackData("Pobyt stały i rezydent - odbiór karty", "/Biala08") },
                        new [] { InlineKeyboardButton.WithCallbackData("Obywatele UE + Polski Dokument Podróży", "/Biala09") }
                    });

                    await _botClient.SendTextMessageAsync(
                        callbackQuery.Message.Chat.Id,
                        "Вы выбрали город Biala Podlaska. Пожалуйста, выберите один из следующих вопросов:",
                        replyMarkup: questionKeyboard
                    );
                    break;
                case "/Biala01":
                case "/Biala02":
                case "/Biala03":
                case "/Biala04":
                case "/Biala05":
                case "/Biala06":
                case "/Biala07":
                case "/Biala08":
                case "/Biala09":

                    foreach(var el in BialaCodeMapping.buttonCodeMapping)
                    {
                        if (string.Equals(el.Value, selectedButton)) {
                            var users = await _userService.GetAllAsync(u => u.TelegramUserId == callbackQuery.From.Id);
                            var user = users.FirstOrDefault();
                            if (user == null || (user != null && !user.Subscriptions.Any(s => s.SubscriptionCode == selectedButton)))
                            {
                                await _userService.SaveSubscription(callbackQuery.Message.Chat.Id, selectedButton);
                                await _botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id,
                                    $"Вы подписались на уведомление {el.Key}");
                            }
                            else
                            {
                                if (user.Subscriptions.Any(s => s.SubscriptionCode == selectedButton))
                                {
                                    await _userService.DeleteSubsription(callbackQuery.Message.Chat.Id, selectedButton);
                                    await _botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id,
                                        $"Вы отписались от уведомления {el.Key}");
                                }
                            }
                        }
                    }

                    break;
                default:
                    await _botClient.SendTextMessageAsync(
                        callbackQuery.Message.Chat.Id,
                        $"Вы выбрали город: {selectedButton}"
                    );
                    break;
            }
        }

        private async Task HandleMessage(Message message)
        {
            _logger.LogInformation($"Received message from {message.Chat.Id}: {message.Text}");

            var mainKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Menu" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false,
                IsPersistent = true // Keyboard is visible
            };

            if (message.Text == "/start")
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Welcome to the bot!", replyMarkup: mainKeyboard);
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Нажмите 'Меню', чтобы выбрать город.");
            }
            else if (message.Text == "Menu")
            {
                var cityKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Gdansk", "Gdansk"),
                        InlineKeyboardButton.WithCallbackData("Biala Podlaska", "Biala Podlaska")
                    }
                    // Можно добавить другие города аналогично
                });

                await _botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    "Выберите город из списка:",
                    replyMarkup: cityKeyboard
                );
            }
            else if (message.Text.StartsWith("/date"))
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, $"Current date: {DateTime.Now}", replyMarkup: mainKeyboard);
            }
        }

        public async Task SendTextMessage(long TelegramUserId, string message)
        {
            await _botClient.SendTextMessageAsync(TelegramUserId, message);
            _logger.LogInformation($"Message sent to user {TelegramUserId} message: {message}");
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
