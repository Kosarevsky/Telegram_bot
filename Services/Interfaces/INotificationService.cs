namespace Services.Interfaces
{
    public interface INotificationService
    {
       // Task SendNotificationAsync(string message);

        Task StartAsync(CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);

    }
}
