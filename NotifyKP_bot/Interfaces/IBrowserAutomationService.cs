
namespace BezKolejki_bot.Interfaces
{
    public interface IBrowserAutomationService
    {
        Task GetAvailableDateAsync(IEnumerable<string> url);
    }
}

