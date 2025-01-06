using Microsoft.Extensions.Hosting;
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
        private readonly long _chatId;
        private readonly IUserService _userService;
        private readonly IEventPublisherService _eventPublisher;
        private readonly IBezKolejkiService _bezKolejkiService;
        private readonly ConcurrentDictionary<long , int> _userMessageCounts = new ();
        private readonly ConcurrentDictionary<long , DateTime> _usersBan = new ();
        private readonly Timer _resetTimer;
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
             _resetTimer = new Timer(_ => ResetMessageCounts(), null, TimeSpan.Zero, TimeSpan.FromMinutes(3));
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
            
            IncrementMessageCount(userId);
            if (MessageCountFromUser(userId) == 10)
            {
                await SendTextMessage(userId, "Слишком много сообщений. Отдохнем пару минут");
            }
            if (MessageCountFromUser(userId) >= 11)
            {
                BanUser(userId, 60);
            }

            if (!IsUserBanned(userId))
            {
                switch (selectedButton)
                {
                    case "Gdansk":
                        var questionKeyboardGdansk = new InlineKeyboardMarkup(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("Zezwolenie na pobyt (stały, czasowy), rezydenta, wymiana karty, dokumenty dla cudzoziemców", "/Gdansk01") }
                        });

                        await SendTextMessage(
                            callbackQuery.Message.Chat.Id,
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

                    case "Olsztyn":
                        var questionKeyboardOlsztyn = new InlineKeyboardMarkup(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("WMUW Karta Polaka", "/OlsztynKP") }
                        });

                        await SendTextMessage(
                            callbackQuery.Message.Chat.Id,
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
                            callbackQuery.Message.Chat.Id,
                            "Вы выбрали город Biala Podlaska. Пожалуйста, выберите операцию:",
                            replyMarkup: questionKeyboardSlupsk
                        );
                        break;
                    case "Moskwa":
                        var questionKeyboardMoskwa = new InlineKeyboardMarkup(new[]
                        {
                            new [] { InlineKeyboardButton.WithCallbackData("FEDERACJA ROSYJSKA. Moskwa. Karta Polaka", "/MoskwaKP") }
                        });

                        await SendTextMessage(
                            callbackQuery.Message.Chat.Id,
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
                            callbackQuery.Message.Chat.Id,
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
                    case "/Rzeszow01":
                    case "/Rzeszow04":
                    case "/Rzeszow06":
                    case "/Gdansk01":
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
                            callbackQuery.Message.Chat.Id,
                            $"Вы выбрали город: {selectedButton}"
                        );
                        break;
                }
            }
        }
        private async Task HandleMessage(Message message)
        {
            var tgUser = new UserModel
            {
                TelegramUserId = message.Chat.Id,
                FirstName = message.Chat.FirstName,
                LastName = message.Chat.LastName,
                UserName = message.Chat.Username,
                Title = message.Chat.Title
            };

            _logger.LogInformation($"Received message from {message.Chat.Id}: {message.Text}");
            IncrementMessageCount(message.Chat.Id);
            if (MessageCountFromUser(message.Chat.Id) == 10)
            {
                BanUser(message.Chat.Id, 60);
            }
            if (MessageCountFromUser(message.Chat.Id) == 11)
            {
                await SendTextMessage(message.Chat.Id, "Слишком много сообщений. Отдохнем пару минут");
            }

            if (!IsUserBanned(message.Chat.Id))
            {

                var mainKeyboard = CreateMainKeyboard();

                var commands = new Dictionary<string, Func<Task>>()
                {
                    {"/start", async () => await HandleStartCommand(tgUser, mainKeyboard) },
                    {"Меню", async () => await HandleMenuCommand(tgUser) },
                    {"/date", async() => await HandleDateCommand(tgUser, mainKeyboard) },
                    {"Подписки", async () => await SendSubscriptionList(message.Chat.Id, mainKeyboard) }
                };

                if (commands.TryGetValue(message.Text, out var commandHandler))
                {
                    await commandHandler();
                }
                else
                {
                    _logger.LogInformation($"Unknown command {message.Text}");
                }
            }
        }

        private async Task ProcessSubscriptionSelection(CallbackQuery callbackQuery)
        {
            var selectedButton = callbackQuery.Data;
            //var nameSubscription = CodeMapping.GetKeyByCode(selectedButton);
            var message = $"{CodeMapping.GetSiteIdentifierByCode(selectedButton)}. {CodeMapping.GetKeyByCode(selectedButton)}";
            if (!string.IsNullOrEmpty(message))
            {
                var users = await _userService.GetAllAsync(u=>u.TelegramUserId == callbackQuery.From.Id);
                var user = users.FirstOrDefault();
                if (user == null || !user.Subscriptions.Any(s => s.SubscriptionCode == selectedButton))
                {
                    await _userService.SaveSubscription(callbackQuery?.Message?.Chat.Id ?? 0 , selectedButton);

                    await SendTextMessage(callbackQuery.Message.Chat.Id, $"Вы подписались на уведомление {message}\n");
                    
                    await _eventPublisher.PublishDatesSavedAsync(selectedButton, 
                        await _bezKolejkiService.GetLastExecutionDatesByCodeAsync(selectedButton), 
                        new List<DateTime>(),
                        callbackQuery.Message.Chat.Id);

                    _logger.LogInformation("Subscribed to DatesSaved event.");
                }
                else
                {
                    await _userService.DeleteSubscription(callbackQuery.Message?.Chat.Id  ?? 0, selectedButton);
                    await SendTextMessage(callbackQuery?.Message?.Chat.Id ?? 0, $"Вы отписались от уведомления\n{message}");
                }
            }
        }

        private async Task SendSubscriptionList(long id,  ReplyKeyboardMarkup mainKeyboard)
        {
            var listSubscription = new List<string>();
            var users = await _userService.GetAllAsync(u=> u.TelegramUserId == id);
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
                            listSubscription.Add($"{listSubscription.Count+1}. {TruncateText(subscriptionName, 67)}");
                        }
                    }

                    var subscriptionsMessage = string.Join(Environment.NewLine, listSubscription);

                    if (subscriptionsMessage.Count() == 0)
                    {
                        await SendTextMessage(id, "У вас нет подписок.\nВыберите город и подпишитесь на услугу", replyMarkup: mainKeyboard);
                    }
                    else
                    {
                        await SendTextMessage(id, $"Перечень активных подписок:  \n{subscriptionsMessage}", replyMarkup: mainKeyboard);
                    }
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

        private async Task HandleMenuCommand(UserModel tgUser)
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

            await SendTextMessage(
                tgUser.TelegramUserId,
                "Выберите город из списка:",
                replyMarkup: cityKeyboard
            );
        }

        private async Task HandleStartCommand(UserModel tgUser, ReplyKeyboardMarkup mainKeyboard)
        {
            var mess = "Welcome to the bot!\n** Бот находится стадии разработки. **\nНажмите 'Меню', чтобы выбрать город.";
            await SendTextMessage(tgUser.TelegramUserId, mess, replyMarkup: mainKeyboard);
            await _userService.UpdateLastNotificationDateAsync(tgUser);
        }

        private ReplyKeyboardMarkup CreateMainKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Меню" , "Подписки"},
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

        public async Task SendTextMessage(long chatId, string messageText, IReplyMarkup replyMarkup = null, CancellationToken cancellationToken = default)
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
            _logger.LogWarning($"User {telegramId} banned in {durationInSecond}");
            _usersBan[telegramId] =  DateTime.UtcNow.AddSeconds(durationInSecond);
        }

        public bool IsUserBanned(long telegramId)
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
