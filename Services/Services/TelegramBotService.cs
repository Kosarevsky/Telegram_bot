﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types.ReplyMarkups;
using Services.Models;
using Telegram.Bot.Exceptions;
using System.Collections.Concurrent;

namespace Services.Services
{
    public class TelegramBotService : IHostedService, ITelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger _logger;
        private readonly IUserService _userService;
        private readonly IEventPublisherService _eventPublisher;
        private readonly IBezKolejkiService _bezKolejkiService;
        private readonly ILocalizationService _lang;
        private readonly ConcurrentDictionary<long , int> _userMessageCounts = new ();
        private readonly ConcurrentDictionary<long , DateTime> _usersBan = new ();
        private readonly Timer _resetTimer;
        private readonly long _adminTlgId;
        private readonly string _donateUrl = string.Empty;
        public TelegramBotService(
            ITelegramBotClient botClient,
            ILogger<TelegramBotService> logger, 
            IUserService userService, 
            IEventPublisherService eventPublisher, 
            IBezKolejkiService bezKolejkiService,
            ILocalizationService lang,
            IConfiguration configuration)
        {
            _botClient = botClient;
            _logger = logger;
            _userService = userService;
            _eventPublisher = eventPublisher;
            _bezKolejkiService = bezKolejkiService;
            _lang = lang;
            _donateUrl = configuration["OtherSettings:DonateUrl"] ?? string.Empty;
            try
            {
                _resetTimer = new Timer(_ => ResetMessageCounts(), null, TimeSpan.Zero, TimeSpan.FromMinutes(3));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize timer: {ex.Message}");
            }

            _adminTlgId = long.TryParse(configuration["Telegram:AdminTelegramId"], out long resParse) ? resParse : 0;
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
                                offset = (update?.Id ?? 0) + 1;
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
            
            IncrementMessageCount(userId);
            if (MessageCountFromUser(userId) == 15)
            {
                await SendTextMessage(userId, "Слишком много сообщений. Отдохнем пару минут");
            }
            if (MessageCountFromUser(userId) >= 16)
            {
                BanUser(userId, 60);
            }

            if (!IsUserBanned(userId))
            {
                switch (selectedButton)
                {
                    case "/Donate":
                        await HandleDonateCommand(userId);
                        break;

                    case "/Subscriptions":
                        await SendSubscriptionList(userId, CreateMainKeyboard());
                        break;

                    case "/StopSubscription":
                        await StopSubscription(userId);
                        break;

                    case "/StartSubscription":
                        await StartSubscription(userId, CreateMainKeyboard());
                        break;  

                    case "Gdansk":
                        var questionKeyboardGdansk = new InlineKeyboardMarkup(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("Zezwolenie na pobyt (stały, czasowy), rezydenta, wymiana karty, dokumenty dla cudzoziemców", "/Gdansk01") },
                            new [] { InlineKeyboardButton.WithCallbackData("Składanie wniosków i dokumentacji do wniosków już złożonych w sprawie obywatelstwa polskiego", "/Gdansk02") },
                        });

                        await SendTextMessage(
                            userId,
                            "Вы выбрали город Gdansk. Пожалуйста, выберите операцию:",
                            replyMarkup: questionKeyboardGdansk
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
                            userId,
                            "Вы выбрали город Biala Podlaska. Пожалуйста, выберите операцию:",
                            replyMarkup: questionKeyboard
                        );
                        break;
                    case "Opole":
                        var questionKeyboardOpole = new InlineKeyboardMarkup(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("Wydawanie dokumentów (karty pobytu, zaproszenia)", "/Opole01") },
                            new [] { InlineKeyboardButton.WithCallbackData("Złożenie wniosku: przez ob. UE i członków ich rodzin/na zaproszenie/o wymianę karty pobytu (w przypadku: zmiany danych umieszczonych w posiadanej karcie pobytu, zmiany wizerunku twarzy, utraty, uszkodzenia) oraz uzupełnianie braków formalnych w tych sprawach", "/Opole02") },
                            new [] { InlineKeyboardButton.WithCallbackData("Karta Polaka – złożenie wniosku o przyznanie Karty Polaka dla dziecka (gdy co najmniej jeden z rodziców posiada/posiadał Kartę Polaka)", "/Opole03") },
                            new [] { InlineKeyboardButton.WithCallbackData("Karta Polaka – złożenie wniosku o przedłużenie ważności Karty Polaka / zmiana danych posiadacza / wydanie duplikatu", "/Opole04") },
                            new [] { InlineKeyboardButton.WithCallbackData("Odbiór Karty Polaka", "/Opole05") }
                        });
                        await SendTextMessage(
                            userId,
                            "Вы выбрали город Opole. Пожалуйста, выберите операцию:",
                            replyMarkup: questionKeyboardOpole
                        );
                        break;
                    case "Rzeszow":
                        var questionKeyboardRzeszow = new InlineKeyboardMarkup(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("1. Odbiór paszportów", "/Rzeszow01") },
                            new [] { InlineKeyboardButton.WithCallbackData("4. Składanie wniosków w sprawach obywatelstwa polskiego (nadanie, zrzeczenie, uznanie, potwierdzenie posiadania) - pokój 326, III piętro", "/Rzeszow04") },
                            new [] { InlineKeyboardButton.WithCallbackData("6. Złożenie wniosku przez obywateli UE oraz członków ich rodzin (NIE DOT. OB. POLSKICH I CZŁONKÓW ICH RODZIN); złożenie wniosku o wymianę dokumentu, przedłużenie wizy; zaproszenie", "/Rzeszow06") }
                        });

                        await SendTextMessage(
                            userId,
                            "Вы выбрали город Rzeszow. Пожалуйста, выберите операцию:",
                            replyMarkup: questionKeyboardRzeszow
                        );
                        break;

                    case "Olsztyn":
                        var questionKeyboardOlsztyn = new InlineKeyboardMarkup(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("WMUW Karta Polaka", "/OlsztynKP") }
                        });

                        await SendTextMessage(
                            userId,
                            "Вы выбрали город Olsztyn. Пожалуйста, выберите операцию:",
                            replyMarkup: questionKeyboardOlsztyn
                        );
                        break;

                    case "Slupsk":
                        var questionKeyboardSlupsk = new InlineKeyboardMarkup(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("Wniosek legalizujący pobyt lub złożenie odcisków palców", "/Slupsk01") },
                            new [] { InlineKeyboardButton.WithCallbackData("Zezwolenia na pracę i zaproszenia", "/Slupsk02") },
                            new [] { InlineKeyboardButton.WithCallbackData("Uzupełnienie dokumentów oraz pozostałe wnioski", "/Slupsk03") }
                        });

                        await SendTextMessage(
                            userId,
                            "Вы выбрали город Slupsk. Пожалуйста, выберите операцию:",
                            replyMarkup: questionKeyboardSlupsk
                        );
                        break;
                    case "Moskwa":
                        var questionKeyboardMoskwa = new InlineKeyboardMarkup(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("FEDERACJA ROSYJSKA. Moskwa. Karta Polaka", "/MoskwaKP") }
                        });

                        await SendTextMessage(
                            userId,
                            "Вы выбрали город Moskwa. Пожалуйста, выберите операцию:",
                            replyMarkup: questionKeyboardMoskwa
                        );
                        break;
                    case "Almaty":
                        var questionKeyboardAlmaty = new InlineKeyboardMarkup(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("KAZACHSTAN. Almaty. Karta Polaka", "/AlmatyKP") }
                        });

                        await SendTextMessage(
                            userId,
                            "Вы выбрали город Almaty. Пожалуйста, выберите операцию:",
                            replyMarkup: questionKeyboardAlmaty
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
                    case "/Opole05":
                    case "/Rzeszow01":
                    case "/Rzeszow04":
                    case "/Rzeszow06":
                    case "/Gdansk01":
                    case "/Gdansk02":
                    case "/OlsztynKP":                  
                    case "/Slupsk01":
                    case "/Slupsk02":
                    case "/Slupsk03":
                    case "/MoskwaKP":
                    case "/AlmatyKP":
                        await ProcessSubscriptionSelection(callbackQuery);
                        break;

                    default:
                        await SendTextMessage(
                            userId,
                            $"Вы выбрали город: {selectedButton}"
                        );
                        break;
                }
            }
        }

        private async Task StartSubscription(long userId, ReplyKeyboardMarkup mainKeyboard)
        {

            var user = await _userService.GetAllAsync(u => u.TelegramUserId == userId);
            var tgUser = user.FirstOrDefault();
            if (tgUser != null)
            {
                await HandleStartCommand(tgUser, mainKeyboard);
            }
        }

        private async Task StopSubscription(long telegramUserId)
        {
            await _userService.DeactivateUserAsync(telegramUserId);
            var DeactivationMessage = "Рассылка сообщений остановлена.";
            await SendTextMessage(telegramUserId, DeactivationMessage);
            _logger.LogInformation($"User {telegramUserId} marked as inactive");
        }

        private async Task HandleMessage(Message message)
        {
            var tgUser = new UserModel
            {
                TelegramUserId = message.Chat.Id,
                FirstName = message.Chat.FirstName,
                LastName = message.Chat.LastName,
                UserName = message.Chat.Username
            };

            _logger.LogInformation($"Received message from {message.Chat.Id}: {message.Text}");
            IncrementMessageCount(message.Chat.Id);
            if (MessageCountFromUser(message.Chat.Id) == 15)
            {
                await SendTextMessage(message.Chat.Id, "Слишком много сообщений. Отдохните пару минут");
            }
            if (MessageCountFromUser(message.Chat.Id) == 16)
            {
                BanUser(message.Chat.Id, 60);
                await SendTextMessage(message.Chat.Id, "Слишком много сообщений. Отдыхаем");
            }

            if (!IsUserBanned(message.Chat.Id))
            {
                var mainKeyboard = CreateMainKeyboard();

                var commands = new Dictionary<string, Func<Task>>()
                {
                    {"/start", async () => await HandleStartCommand(tgUser, mainKeyboard) },
                    {"Menu", async () => await HandleMenuCommand(tgUser.TelegramUserId, mainKeyboard) },
                    {"/date", async() => await HandleDateCommand(tgUser, mainKeyboard) },
                    {"Info", async () => await HandleSubscriptionCommand(tgUser, mainKeyboard) },

                };

                if (commands.TryGetValue(message?.Text ?? string.Empty, out var commandHandler))
                {
                    await commandHandler();
                }
                else
                {
                    _logger.LogInformation($"Unknown command {message?.Text}");
                }
            }
        }

        private async Task ProcessSubscriptionSelection(CallbackQuery callbackQuery)
        {
            var telegramUserId = callbackQuery.Message?.Chat.Id ?? 0;
            var selectedButton = callbackQuery.Data ?? string.Empty;
            var message = $"{CodeMapping.GetSiteIdentifierByCode(selectedButton)}. {CodeMapping.GetKeyByCode(selectedButton)}";
            if (!string.IsNullOrEmpty(message))
            {
                var users = await _userService.GetAllAsync(u=>u.TelegramUserId == callbackQuery.From.Id);
                var user = users.FirstOrDefault();
                if (user == null || !user.Subscriptions.Any(s => s.SubscriptionCode == selectedButton))
                {
                    await _userService.SaveSubscription(telegramUserId , selectedButton);

                    await SendTextMessage(telegramUserId, $"Вы подписались на уведомление {message}\n");
                    
                    await _eventPublisher.PublishDatesSavedAsync(selectedButton, 
                        await _bezKolejkiService.GetLastExecutionDatesByCodeAsync(selectedButton), 
                        new List<DateTime>(),
                        telegramUserId);

                    _logger.LogInformation("Subscribed to DatesSaved event.");
                }
                else
                {
                    await _userService.DeleteSubscription(telegramUserId, selectedButton);
                    message = $"Вы отписались от уведомления {message}";
                    if (user.Subscriptions.Count <= 1)
                    {
                        await _userService.DeactivateUserAsync(telegramUserId);
                        message += "\nУ вас нет активных подписок. \nРассылка сообщений остановлена.";
                        _logger.LogInformation($"User {telegramUserId} {user.UserName} marked as inactive");
                    }
                    await SendTextMessage(telegramUserId, message);
                }
            }
        }

        private async Task HandleSubscriptionCommand(UserModel tgUser, ReplyKeyboardMarkup mainKeyboard)
        {
            var users = await _userService.GetAllAsync(u => u.TelegramUserId == tgUser.TelegramUserId);
            var user = users.FirstOrDefault();
            if (user != null)
            {
                var language = user.Language ?? "en"; 
                var buttons = new List<InlineKeyboardButton[]>
                {
                    new[] { InlineKeyboardButton.WithCallbackData(_lang.GetText(language, "HelpButton"), "/Donate") },
                    new[] { InlineKeyboardButton.WithCallbackData(_lang.GetText(language, "SubscriptionsButton"), "/Subscriptions") },
                }
            ;
                buttons.Add(user.IsActive
                    ? new[] { InlineKeyboardButton.WithCallbackData(_lang.GetText(language, "StopNotificationsButton"), "/StopSubscription") }
                    : new[] { InlineKeyboardButton.WithCallbackData(_lang.GetText(language, "StartNotificationsButton"), "/StartSubscription") }
                    );
                var subscriptionKeyboard = new InlineKeyboardMarkup(buttons);

                await SendTextMessage(user.TelegramUserId, _lang.GetText(language, "HelpMessage"), replyMarkup: subscriptionKeyboard);
            }
        }

        private async Task SendSubscriptionList(long telegramUserId,  ReplyKeyboardMarkup mainKeyboard)
        {
            var listSubscription = new List<string>();
            var users = await _userService.GetAllAsync(u=> u.TelegramUserId == telegramUserId);
            if (users != null)
            {
                var user = users.FirstOrDefault();
                var userSubscription = user?.Subscriptions.ToList();

                if (user != null && userSubscription != null)
                {
                    foreach (var el in userSubscription)
                    {
                        var subscriptionName = $"{CodeMapping.GetSiteIdentifierByCode(el.SubscriptionCode)}. {CodeMapping.GetKeyByCode(el.SubscriptionCode)}";
                        if (!string.IsNullOrEmpty(subscriptionName))
                        {
                            listSubscription.Add($"{listSubscription.Count+1}. {TruncateText(subscriptionName, 77)}");
                        }
                    }

                    var subscriptionsMessage = string.Join(Environment.NewLine, listSubscription);

                    await SendTextMessage(telegramUserId, user.Subscriptions.Count > 0
                        ? $"Перечень активных подписок:  \n{subscriptionsMessage}"
                        : "У вас нет подписок.\nВыберите город и подпишитесь на услугу"
                        , replyMarkup: mainKeyboard);
                    
                }
            }
        }
        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || maxLength <= 0)
                return string.Empty;

            return text.Length > maxLength
                ? text.Substring(0, maxLength - 3) + "..."
                : text;
        }

        private async Task HandleDateCommand(UserModel tgUser, ReplyKeyboardMarkup mainKeyboard)
        {
            await SendTextMessage(tgUser.TelegramUserId, $"Current date: {DateTime.Now}", replyMarkup: mainKeyboard);
        }
        private async Task HandleDonateCommand(long telegramUserId)
        {
            var users = await _userService.GetAllAsync(u => u.TelegramUserId == telegramUserId);
            var user = users.FirstOrDefault();
            if (user != null)
            {
                var language = user.Language ?? "en";

                var donateButton = InlineKeyboardButton.WithUrl(_lang.GetText(language, "DonateButton"), _donateUrl);
                var donateKeyboard = new InlineKeyboardMarkup(donateButton);

                var message = string.Join("\n", _lang.GetText(language, "DonateMessage"), _donateUrl);
                await SendTextMessage(telegramUserId, message, replyMarkup: donateKeyboard);
            }
        }

        private async Task HandleMenuCommand(long telegramUserId, ReplyKeyboardMarkup mainKeyboard)
        {
            var cityKeyboard = new InlineKeyboardMarkup(new[]
            {
                    new [] { InlineKeyboardButton.WithCallbackData("Gdansk", "Gdansk") },
                    new [] { InlineKeyboardButton.WithCallbackData("Biala Podlaska", "Biala Podlaska") },
                    new [] { InlineKeyboardButton.WithCallbackData("Opole", "Opole") },
                    new [] { InlineKeyboardButton.WithCallbackData("Rzeszow", "Rzeszow") },
                    new [] { InlineKeyboardButton.WithCallbackData("Olsztyn", "Olsztyn") },
                    new [] { InlineKeyboardButton.WithCallbackData("Slupsk", "Slupsk") },
                    new [] { InlineKeyboardButton.WithCallbackData("Moskwa", "Moskwa") },
                    new [] { InlineKeyboardButton.WithCallbackData("Almaty", "Almaty") },
            });

            await SendTextMessage(telegramUserId, "Выберите город из списка:", replyMarkup: cityKeyboard);
        }

        private async Task HandleStartCommand(UserModel tgUser, ReplyKeyboardMarkup mainKeyboard)
        {
            var mess = "Welcome to the bot!\n** Бот находится в стадии разработки. **\nНажмите 'Menu', чтобы выбрать город.";
            await SendTextMessage(tgUser.TelegramUserId, mess, replyMarkup: mainKeyboard);
            await _userService.UpdateLastNotificationDateAsync(tgUser);
        }

        private ReplyKeyboardMarkup CreateMainKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Menu" , "Info"},
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false,
                IsPersistent = true
            };
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Telegram Bot...");
            return Task.CompletedTask;
        }

        public async Task SendTextMessage(long chatId, string messageText, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
        {
            try
            {
                    await _botClient.SendMessage(chatId, messageText, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 403 || ex.ErrorCode == 400)
            {
                _logger.LogWarning($"User {chatId} blocked the bot. Updating user status to inactive.");
                await DeactivateUserAsync(chatId);
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 429)
            {
                int retryAfter = ex?.Parameters?.RetryAfter ?? 5;
                _logger.LogWarning($"Too many requests. Retrying after {retryAfter} seconds.");
                await Task.Delay(retryAfter * 1000, cancellationToken);
                await SendTextMessage(chatId, messageText, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Request timed out.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while sending message: {ex.Message}");
            }
        }
        public async Task SendAdminTextMessage(string messageText, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
        {
            await SendTextMessage(_adminTlgId, messageText, replyMarkup, cancellationToken);
        }

        private async Task DeactivateUserAsync(long chatId)
        {
            await _userService.DeactivateUserAsync(chatId);
        }

        public void IncrementMessageCount(long telegramId)
        {
            _userMessageCounts.AddOrUpdate(telegramId, 1, (id, count) => count + 1);
        }

        public int MessageCountFromUser(long telegramId)
        {
            return _userMessageCounts.TryGetValue(telegramId, out var count) ? count :0;
        }

        public void BanUser(long telegramId, int durationInSecond)
        {
            _logger.LogWarning($"User {telegramId} banned for {durationInSecond} seconds. Message count: {_userMessageCounts[telegramId]}");
            _usersBan[telegramId] =  DateTime.UtcNow.AddSeconds(durationInSecond);
        }

        private bool IsUserBanned(long telegramId)
        { 
            if (_usersBan.TryGetValue(telegramId, out var banUntil))
            {
                if (DateTime.UtcNow < banUntil)
                {
                    _logger.LogWarning($"*** User banned {telegramId}. MessageCount: {_userMessageCounts[telegramId]} {banUntil.ToShortTimeString()}");
                    return true; //User is banned
                }
                else
                {
                    _userMessageCounts[telegramId] = 0;
                    _usersBan.TryRemove(telegramId, out _);
                }
            }
            return false;
        }

        public void ResetMessageCounts()
        {
            foreach (var userId in _userMessageCounts.Keys)
            {
                _userMessageCounts[userId] = 0;
            }
        }
    }
}
