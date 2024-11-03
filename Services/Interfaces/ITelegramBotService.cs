namespace Services.Interfaces
{
    public interface ITelegramBotService
    {
        Task SendTextMessage(long telegramUserId, string message, string code, List<DateTime> dates);

        Task StartAsync(CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);

    }
}
