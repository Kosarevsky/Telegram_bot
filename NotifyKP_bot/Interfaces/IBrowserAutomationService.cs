
namespace NotifyKP_bot.Interfaces
{
    public interface IBrowserAutomationService
    {
        Task<List<DateTime>> GetAvailableDateAsync(string url);
    }
}
