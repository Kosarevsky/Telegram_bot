using Telegram.Bot.Types.ReplyMarkups;

namespace Services.Interfaces
{
    public interface ITelegramBotService
    {
        //Task SendTextMessage(long telegramUserId, string message);
        Task SendTextMessage(long chatId, string messageText, IReplyMarkup replyMarkup = null, CancellationToken cancellationToken = default);

        Task StartAsync(CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);

    }
}
