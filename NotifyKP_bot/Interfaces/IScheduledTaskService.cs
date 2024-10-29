
namespace NotifyKP_bot.Interfaces
{
    public interface IScheduledTaskService
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
        void Dispose();
    }
}
