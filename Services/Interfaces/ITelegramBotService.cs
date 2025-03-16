using Telegram.Bot.Types.ReplyMarkups;

namespace Services.Interfaces
{
    public interface ITelegramBotService
    {
        Task SendTextMessage(long chatId, string messageText, ReplyMarkup replyMarkup = null, CancellationToken cancellationToken = default);
        Task SendAdminTextMessage(string messageText, ReplyMarkup replyMarkup = null, CancellationToken cancellationToken = default);

        Task StartAsync(CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);

    }
}
