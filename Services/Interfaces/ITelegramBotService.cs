namespace Services.Interfaces
{
    public interface ITelegramBotService
    {
        Task SendTextMessage(long TelegramUserId, string message);

        Task StartAsync(CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);

    }
}
