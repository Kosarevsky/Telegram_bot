using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types.ReplyMarkups;
using Services.Models;
using Telegram.Bot.Exceptions;

namespace Services.Services
{
    public class TelegramBotService : IHostedService, ITelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger _logger;
        private readonly long _chatId;
        private readonly IUserService _userService;
        private readonly IEventPublisherService _eventPublisher;
        private readonly IBezKolejkiService _bezKolejkiService;
        public TelegramBotService(
            ITelegramBotClient botClient,
            ILogger<TelegramBotService> logger,
            IConfiguration configuration,
            IUserService userService,
            IEventPublisherService eventPublisher,
            IBezKolejkiService bezKolejkiService)
        {
            _botClient = botClient;
            _logger = logger;
            _chatId = long.Parse(configuration["Telegram:ChatId"]);
            _userService = userService;
            _eventPublisher = eventPublisher;
            _bezKolejkiService = bezKolejkiService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Telegram Bot...");

            var offset = 0;

            try
            {
                var me = await _botClient.GetMe();
                _logger.LogInformation($"Bot ID: {me.Id}, Bot Name: {me.Username}");

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var updates = await _botClient.GetUpdates(offset, cancellationToken: cancellationToken);
                        foreach (var update in updates)
                        {
                            try
                            {
                                if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                                {
                                    if (update?.Message?.Text != null)
                                    {
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
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error handling update");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error retrieving updates from Telegram API");
                    }

                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error initializing Telegram bot");
            }
        }

        private async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            var selectedButton = callbackQuery.Data;
            var userId = callbackQuery.From.Id;
            _logger.LogInformation($"User {userId} selected button: {selectedButton}");

            switch (selectedButton)
            {
                case "Gdansk":
                    await SendTextMessage(
                        callbackQuery.Message.Chat.Id,
                        "Гданьск скоро будет. Вы может ускорить процесс купив админу чашечку кофе."
                        );
                    break;
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

                    await SendTextMessage(
                        callbackQuery.Message.Chat.Id,
                        "Вы выбрали город Biala Podlaska. Пожалуйста, выберите операцию:",
                        replyMarkup: questionKeyboard
                    );
                    break;
                case "Opole":
                    var questionKeyboardOpole = new InlineKeyboardMarkup(new[]
                    {
                        new [] { InlineKeyboardButton.WithCallbackData("Wydawanie dokumentów (karty pobytu, zaproszenia", "/Opole01") },
                        new [] { InlineKeyboardButton.WithCallbackData("Złożenie wniosku: przez ob. UE i członków ich rodzin/na zaproszenie/o wymianę karty pobytu (w przypadku: zmiany danych umieszczonych w posiadanej karcie pobytu, zmiany wizerunku twarzy, utraty, uszkodzenia) oraz uzupełnianie braków formalnych w tych sprawach", "/Opole02") },
                        new [] { InlineKeyboardButton.WithCallbackData("Karta Polaka - złożenie wniosku o przyznanie Karty Polaka", "/Opole03") },
                        new [] { InlineKeyboardButton.WithCallbackData("Karta Polaka - złożenie wniosku o wymianę / przedłużenie / wydanie duplikatu / odbiór", "/Opole04") }
                    });

                    await SendTextMessage(
                        callbackQuery.Message.Chat.Id,
                        "Вы выбрали город Opole. Пожалуйста, выберите операцию:",
                        replyMarkup: questionKeyboardOpole
                    );
                    break;
                case "Rzeszow":
                    var questionKeyboardRzeszow = new InlineKeyboardMarkup(new[]
                    {
                        new [] { InlineKeyboardButton.WithCallbackData("1. Odbiór paszportów)", "/Rzeszow01") },
                        new [] { InlineKeyboardButton.WithCallbackData("4. Składanie wniosków w sprawach obywatelstwa polskiego (nadanie, zrzeczenie, uznanie, potwierdzenie posiadania) - pokój 326, III piętro", "/Rzeszow04") },
                        new [] { InlineKeyboardButton.WithCallbackData("6. Złożenie wniosku przez obywateli UE oraz członków ich rodzin (NIE DOT. OB. POLSKICH I CZŁONKÓW ICH RODZIN); złożenie wniosku o wymianę dokumentu, przedłużenie wizy; zaproszenie", "/Rzeszow06") }
                    });

                    await SendTextMessage(
                        callbackQuery.Message.Chat.Id,
                        "Вы выбрали город Rzeszow. Пожалуйста, выберите операцию:",
                        replyMarkup: questionKeyboardRzeszow
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
                case "/Opole01":
                case "/Opole02":
                case "/Opole03":
                case "/Opole04":
                case "/Rzeszow01":
                case "/Rzeszow04":
                case "/Rzeszow06":
                    await ProcessSubscriptionSelection(callbackQuery);
                    break;
                default:
                    await SendTextMessage(
                        callbackQuery.Message.Chat.Id,
                        $"Вы выбрали город: {selectedButton}"
                    );
                    break;
            }
        }

        private async Task ProcessSubscriptionSelection(CallbackQuery callbackQuery)
        {
            var selectedButton = callbackQuery.Data;
            var nameSubscription = CodeMapping.GetKeyByCode(selectedButton);
            if (!string.IsNullOrEmpty(nameSubscription))
            {
                var user = await _userService.GetByTelegramId(callbackQuery.From.Id);

                if (user == null || !user.Subscriptions.Any(s => s.SubscriptionCode == selectedButton))
                {
                    await _userService.SaveSubscription(callbackQuery.Message.Chat.Id, selectedButton);
                    await SendTextMessage(callbackQuery.Message.Chat.Id, $"Вы подписались на уведомление {nameSubscription}\nБот находится в тестовом режиме. \nПрошу не судить строго :)\n");
                    
                    await _eventPublisher.PublishDatesSavedAsync(selectedButton, 
                        await _bezKolejkiService.GetLastExecutionDatesByCodeAsync(selectedButton), 
                        new List<DateTime>(),
                        callbackQuery.Message.Chat.Id);

                    _logger.LogInformation("Subscribed to DatesSaved event.");
                }
                else
                {
                    await _userService.DeleteSubscription(callbackQuery.Message.Chat.Id, selectedButton);
                    await SendTextMessage(callbackQuery.Message.Chat.Id, $"Вы отписались от уведомления\n{nameSubscription}");
                }
            }
        }

        private async Task SendSubscriptionList(long telegramUserId)
        {
            var listSubscription = new List<string>();
            var user = await _userService.GetByTelegramId(telegramUserId);
            var userSubscription = user?.Subscriptions.ToList();

            if (user != null && userSubscription != null)
            {
                foreach (var el in userSubscription)
                {
                    var subscriptionName = $"{CodeMapping.GetSiteIdentifierByCode(el.SubscriptionCode)}. {CodeMapping.GetKeyByCode(el.SubscriptionCode)}";
                    if (!string.IsNullOrEmpty(subscriptionName))
                    {
                        listSubscription.Add(TruncateText(subscriptionName, 47));
                    }
                }

                var subscriptionsMessage = string.Join(Environment.NewLine, listSubscription);

                if (subscriptionsMessage.Length == 0)
                {
                    await SendTextMessage(telegramUserId, "У вас нет подписок.\nВыберите город и подпишитесь на услугу");
                }
                else
                {
                    await SendTextMessage(telegramUserId, $"Перечень активных подписок:  \n{subscriptionsMessage}");
                }
            }
        }
        public static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || maxLength <= 0)
                return string.Empty;

            return text.Length > maxLength
                ? text.Substring(0, maxLength - 3) + "..."
                : text;
        }

        private async Task HandleMessage(Message message)
        {
            _logger.LogInformation($"Received message from {message.Chat.Id}: {message.Text}");

            var mainKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Menu" , "Подписки"},
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false,
                IsPersistent = true 
            };

            if (message.Text == "/start")
            {
                var mess = "Welcome to the bot!\n** Бот находится стадии разработки. **\nНажмите 'Меню', чтобы выбрать город.";
                await SendTextMessage(message.Chat.Id, mess, replyMarkup: mainKeyboard);
                await _userService.UpdateLastNotificationDateAsync(message.Chat.Id);
            }
            else if (message.Text == "Menu")
            {
                var cityKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Gdansk", "Gdansk"),
                        InlineKeyboardButton.WithCallbackData("Biala Podlaska", "Biala Podlaska"),
                        InlineKeyboardButton.WithCallbackData("Opole", "Opole"),
                        InlineKeyboardButton.WithCallbackData("Rzeszow", "Rzeszow")
                    }
                });

                await SendTextMessage(
                    message.Chat.Id,
                    "Выберите город из списка:",
                    replyMarkup: cityKeyboard
                );
            }
            else if (message.Text.StartsWith("/date"))
            {
                await SendTextMessage(message.Chat.Id, $"Current date: {DateTime.Now}", replyMarkup: mainKeyboard);
            }
            else if (message.Text == "Подписки") {
                await SendSubscriptionList(message.Chat.Id);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Telegram Bot...");
            return Task.CompletedTask;
        }

        public async Task SendTextMessage(long chatId, string messageText, IReplyMarkup replyMarkup = null, CancellationToken cancellationToken = default)
        {
            try
            {
                    await _botClient.SendMessage(chatId, messageText, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 429)
            {
                int retryAfter = ex.Parameters.RetryAfter ?? 5;
                _logger.LogWarning($"Too many requests. Retrying after {retryAfter} seconds.");
                await Task.Delay(retryAfter * 1000, cancellationToken);
                await SendTextMessage(chatId, messageText, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            }
        }
    }
}
