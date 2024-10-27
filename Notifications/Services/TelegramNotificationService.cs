using Notifications.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace Notifications.Services
{
    public class TelegramNotificationService : INotificationService
    {
        private readonly string _botToken;
        private readonly long _chatId;

        public TelegramNotificationService(string botToken, long chatId)
        {
            _botToken = botToken;
            _chatId = chatId;
        }

        public async Task SendNotificationAsync(string message)
        {
            var botClient = new TelegramBotClient(_botToken);
            ReceiverOptions receiverOptions = new ReceiverOptions()
            {
                AllowedUpdates = []
            };
            await botClient.SendTextMessageAsync(_chatId, message);
        }


    }
}
